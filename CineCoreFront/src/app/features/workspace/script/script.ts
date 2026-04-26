import { Component, ElementRef, ViewChild, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';
import { Subject} from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { ActivatedRoute, Router } from '@angular/router';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';


type BlockType = 'scene_heading' | 'action' | 'character' | 'dialogue' | 'parenthetical' | 'transition' | 'shot';
type ViewMode = 'edit' | 'breakdown' | 'read';
type SceneViewMode = 'single' | 'all';

interface ScriptBlock {
  id: string;
  type: BlockType;
  content: string;
  charId?: string;
  color?: string;
  sceneCode?: string;
  sceneId?: number;
  charName?: string; // ДОДАНО: Для прив'язки репліки до конкретного імені
}

@Component({
  selector: 'app-script',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './script.html',
  styleUrl: './script.scss'
})
export class Script implements OnInit {
  @ViewChild('scriptPaper') scriptPaper!: ElementRef;
  private api = inject(Api);
  private cdr= inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router); // Додайте цей ін'єкт


  projectId: number = 0;
  sceneId: number = 0;
  sceneNotes: string = '';

  private autosaveSubject = new Subject<void>();
  notesAutosaveSubject = new Subject<string>();

  // --- СТАНИ UI ---
  isLeftOpen = true;
  isRightOpen = true;
  viewMode: ViewMode = 'breakdown';
  sceneViewMode: SceneViewMode = 'single';
  showLinesInRead = true;
  rawScriptText = '';
  openTypeMenuId: string | null = null;
  currentUserRole: string = 'none';
  canEdit: boolean = false;

  @ViewChild('rawEditor') rawEditorRef!: ElementRef;

  scenes: any[] = [];
  activeCharacters: any[] = [];
  blocks: ScriptBlock[] = [];

  myApprovedRoleIds: Set<number> = new Set(); // Може бути кілька затверджених ролей

  // --- РЕСУРСИ (Права панель) ---
  linkedRoles: any[] = [];
  linkedLocations: any[] = [];
  linkedProps: any[] = [];

  availableResources: { roles: any[], locations: any[], props: any[] } = { roles: [], locations: [], props: [] };

  isResourceModalOpen = false;
  resourceModalType: 'role' | 'location' | 'prop' = 'role';
  resourceSearch = '';

  openCharMenuId: string | null = null;
  highlightedCharName: string | null = null;

  toggleHighlight(charName: string) {
    this.highlightedCharName = this.highlightedCharName === charName ? null : charName;
  }

  ngOnInit() {
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');

      // ПРИМУСОВИЙ РЕЖИМ ДЛЯ АКТОРІВ
      if (this.currentUserRole === 'actor') {
        this.viewMode = 'read';
        this.sceneViewMode = 'all'; // Акторам зручніше бачити весь скрипт
        // ✅ findMyApprovedRole викликається в paramMap підписці, коли projectId вже є
        if (this.projectId) {
          this.findMyApprovedRole();
          this.loadData();
        }
      }
      this.cdr.detectChanges();
    });

    // Залишаємо ТІЛЬКИ ОДНУ підписку на paramMap!
    this.route.parent?.paramMap.subscribe(params => {
      const pid = params.get('id');
      if (pid) {
        this.projectId = Number(pid);
        this.loadScenesList(); // Це викличе selectScene, який тепер правильно завантажить дані

        this.api.getProjectResources(this.projectId).subscribe({
          next: (res) => this.availableResources = res
        });

        // ✅ Викликаємо тут, коли projectId вже встановлено
        if (this.currentUserRole === 'actor') {
          this.findMyApprovedRole();
        }
      }
    });

    // НАЛАШТУВАННЯ АВТОЗБЕРЕЖЕННЯ
    this.autosaveSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.performAutosave();
    });

    this.notesAutosaveSubject.pipe(debounceTime(2000)).subscribe((notes) => {
      if (this.canEdit) { // Зберігаємо нотатки тільки якщо є права
        this.api.updateSceneNotes(this.sceneId, notes).subscribe();
      }
    });
  }

  loadScenesList() {
    this.api.getProjectScenes(this.projectId).subscribe({
      next: (data) => {
        this.scenes = data;
        // Якщо в проекті є сцени, автоматично відкриваємо першу
        if (this.scenes.length > 0 && this.sceneId === 0) {
          this.selectScene(this.scenes[0].id);
        }
      },
      error: (err) => console.error('Failed to load scenes', err)
    });
  }

  // script.ts

  selectScene(id: number) {
    // Перевіряємо, чи це перше завантаження (коли sceneId ще 0)
    const isInitialLoad = this.sceneId === 0;

    if (this.sceneViewMode === 'all') {
      this.sceneId = id;
      if (isInitialLoad || this.blocks.length === 0) {
        this.loadData();
      } else {
        this.scrollToScene(id);
      }
      return;
    }

    // ✅ FIX: Запам'ятовуємо СТАРИЙ sceneId ДО того як змінити його.
    // Це вирішує race condition: performManualSave має зберігати вміст
    // ПОПЕРЕДНЬОЇ сцени, а не нової.
    const previousSceneId = this.sceneId;

    // ✅ FIX: Скидаємо debounce щоб "застряглий" autosave не прилетів
    // вже після того як ми переключились на нову сцену.
    this.autosaveSubject.complete();
    // Перестворюємо Subject (після complete він мертвий)
    this.autosaveSubject = new Subject<void>();
    this.autosaveSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.performAutosave();
    });

    // Тепер безпечно оновлюємо sceneId
    this.sceneId = id;

    // Зберігаємо вміст ПОПЕРЕДНЬОЇ сцени з правильним previousSceneId
    if (this.canEdit && !isInitialLoad) {
      this.performManualSave(previousSceneId);
    }

    this.loadData();
  }

  scrollToScene(id: number) {
    const element = document.getElementById(`scene-anchor-${id}`);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  onScroll(event: Event) {
    if (this.sceneViewMode !== 'all') return;

    const container = event.target as HTMLElement;
    const headings = container.querySelectorAll('.scene-divider[id]');

    let currentActiveId = this.sceneId;

    headings.forEach((header: any) => {
      const rect = header.getBoundingClientRect();
      // Якщо заголовок піднявся до верху панелі (з невеликим запасом 100px)
      if (rect.top <= 200) {
        const idStr = header.id.replace('scene-anchor-', '');
        currentActiveId = Number(idStr);
      }
    });

    if (this.sceneId !== currentActiveId) {
      this.sceneId = currentActiveId;
      this.cdr.detectChanges(); // Оновлюємо підсвітку зліва
    }
  }

  // ДОДАЙТЕ В script.ts

  deleteScene(event: Event, id: number) {
    event.stopPropagation(); // ВАЖЛИВО: щоб не вибрати сцену при кліку на "видалити"

    if (!confirm('Are you sure you want to delete this scene? All text within it will be lost.')) {
      return;
    }

    this.api.deleteScene(id).subscribe({
      next: () => {
        // 1. Видаляємо з локального списку
        const index = this.scenes.findIndex(s => s.id === id);
        if (index !== -1) {
          this.scenes.splice(index, 1);

          // 2. Оновлюємо номери SequenceNum локально для UI
          this.scenes.forEach((s, i) => s.sequenceNum = i + 1);
        }

        // 3. Якщо видалили поточну активну сцену - перемикаємось на іншу
        if (this.sceneId === id) {
          this.sceneId = 0;
          this.blocks = [];
          if (this.scenes.length > 0) {
            // Відкриваємо найближчу сцену (нову на цьому ж індексі або попередню)
            const nextToSelect = this.scenes[index] || this.scenes[index - 1];
            if (nextToSelect) this.selectScene(nextToSelect.id);
          }
        }
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to delete scene', err)
    });
  }

  performManualSave(sceneId: number = this.sceneId) {
    if (this.viewMode === 'edit') {
      this.parseRawTextToBlocks();
    }

    // НОВЕ: Перевіряємо локально, чи є в цій сцені більше одного заголовка
    const willSplit = this.blocks.filter(b => b.type === 'scene_heading').length > 1;

    this.api.autoSaveScript(sceneId, this.projectId, this.blocks).subscribe({
      next: (res: any) => {
        // НОВЕ: Якщо бекенд розрізав сцену АБО ми передбачили це локально
        if (res.wasSplit || willSplit) {
          // 1. Оновлюємо ліву панель (з'явилися нові сцени зі зсунутими номерами)
          this.loadScenesList();

          // 2. Перемикаємо режими, щоб показати користувачу результат
          this.sceneViewMode = 'all';  // Вмикаємо Full Script
          this.viewMode = 'breakdown'; // Виходимо з режиму набору тексту

          // 3. Завантажуємо весь оновлений сценарій
          this.loadData();
        } else {
          // Стара логіка: звичайне збереження без розрізання
          this.patchSceneDuration(sceneId, res.estimatedDuration);
        }
      }
    });
  }

  // Оновлює назву сцени у списку зліва на основі блоку scene_heading
  private syncSidebarTitle() {
    // Знаходимо блок заголовка (зазвичай він перший)
    const headingBlock = this.blocks.find(b => b.type === 'scene_heading');
    if (headingBlock) {
      // Знаходимо цю ж сцену у списку зліва
      const sidebarScene = this.scenes.find(s => s.id === this.sceneId);
      if (sidebarScene) {
        sidebarScene.sluglineText = headingBlock.content;
      }
    }
  }

  loadData() {
    if (this.sceneViewMode === 'all') {
      this.loadFullScript();
    } else {
      this.loadSceneScript();
    }
  }

  setSceneViewMode(mode: SceneViewMode) {
    if (this.sceneViewMode === mode) return;

    // Захист: якщо ми в режимі Write, не даємо увімкнути Full Script
    if (mode === 'all' && this.viewMode === 'edit') return;

    this.sceneViewMode = mode;
    this.loadData();
  }

  loadFullScript() {
    this.api.getFullScript(this.projectId).subscribe({
      next: (data: any) => {
        this.blocks = data.blocks;

        // ДОДАНО: Тепер ми підтягуємо всі ресурси проекту
        this.linkedLocations = data.linkedLocations || [];
        this.linkedProps = data.linkedProps || [];

        this.extractCharactersFromBlocks();
        this.updateColors();
        this.cdr.detectChanges();
      }
    });
  }

  setViewMode(mode: ViewMode) {
    // 1. Якщо ми виходимо з режиму редагування — зберігаємо зміни
    if (this.viewMode === 'edit') {
      this.parseRawTextToBlocks();
      this.performAutosave();
    }

    this.viewMode = mode;

    // 2. Якщо ми заходимо в режим Write
    if (mode === 'edit') {
      if (this.sceneViewMode === 'all') {
        // Якщо був Full Script — перемикаємо на Single і завантажуємо повні дані сцени (включаючи праву панель)
        this.sceneViewMode = 'single';
        this.loadSceneScript();
      } else {
        // Якщо вже був Single — просто готуємо текст для редактора
        this.buildRawTextFromBlocks();
        setTimeout(() => {
          if (this.rawEditorRef?.nativeElement) {
            this.rawEditorRef.nativeElement.innerText = this.rawScriptText;
          }
        }, 0);
      }
    }
  }

  loadSceneScript() {
    this.api.getSceneScript(this.sceneId).subscribe({
      next: (data: any) => {
        this.blocks = data.blocks;       // Беремо блоки з нового об'єкта
        this.sceneNotes = data.notes || ''; // Беремо нотатки

        this.linkedRoles = data.linkedRoles || [];
        this.linkedLocations = data.linkedLocations || [];
        this.linkedProps = data.linkedProps || [];

        this.syncSidebarTitle();
        this.extractCharactersFromBlocks();
        this.updateColors();

        if (this.viewMode === 'edit') {
          this.buildRawTextFromBlocks();
          if (this.rawEditorRef?.nativeElement) {
            this.rawEditorRef.nativeElement.innerText = this.rawScriptText;
          }
        }
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load script', err)
    });
  }

  performAutosave() {
    if (!this.canEdit || this.sceneViewMode === 'all') return;

    // НОВЕ: Перевіряємо, чи з'явився другий заголовок під час набору тексту
    const willSplit = this.blocks.filter(b => b.type === 'scene_heading').length > 1;

    this.api.autoSaveScript(this.sceneId, this.projectId, this.blocks).subscribe({
      next: (res: any) => {
        if (res.wasSplit || willSplit) {
          // Сцена розрізана! Робимо те саме, що і при ручному збереженні
          this.loadScenesList();
          this.sceneViewMode = 'all';
          this.viewMode = 'breakdown';
          this.loadData();
        } else {
          // Звичайне фонове збереження
          this.extractCharactersFromBlocks();
          this.patchSceneDuration(this.sceneId, res.estimatedDuration);
        }
      },
      error: (err) => console.error('Autosave failed', err)
    });
  }

  // Оновлює estimatedDuration у локальному списку сцен без перезавантаження
  private patchSceneDuration(sceneId: number, duration: string | null) {
    const scene = this.scenes.find(s => s.id === sceneId);
    if (scene) {
      scene.estimatedDuration = duration;
      this.cdr.detectChanges();
    }
  }

  extractCharactersFromBlocks() {
    const textChars = [...new Set(this.blocks.filter(b => b.type === 'character').map(b => b.content.trim().toUpperCase()))];
    const merged = [];

    for (const tc of textChars) {
      const projectRole = this.availableResources.roles.find(r => r.name.toUpperCase() === tc);
      merged.push({
        id: projectRole?.id || null,
        name: tc,
        color: projectRole?.color || '#3AB9A0',
        initials: tc.charAt(0),
        isAuto: projectRole ? projectRole.isAutoGenerated : true
      });
    }

    // Додаємо ролі явно прив'язані до сцени через панель (linkedRoles),
    // яких ще немає в тексті сценарію
    for (const linked of this.linkedRoles) {
      if (!merged.some(m => m.id === linked.id)) {
        merged.push({
          id: linked.id,
          name: linked.name,
          color: linked.color,
          initials: linked.name.charAt(0),
          isAuto: false
        });
      }
    }

    this.activeCharacters = merged;
  }

  toggleLeft() { this.isLeftOpen = !this.isLeftOpen; }
  toggleRight() { this.isRightOpen = !this.isRightOpen; }

  onRawInput(event: Event) {
    const el = event.target as HTMLElement;
    this.rawScriptText = el.innerText.replace(/\n{3,}/g, '\n\n');
    this.parseRawTextToBlocks();
    this.autosaveSubject.next();
  }

  // 1. Геттер для формування красивого коду сцени (напр. SC-002)
  get currentSceneCode(): string {
    const scene = this.scenes.find(s => s.id === this.sceneId);
    if (scene) {
      // Робить з 1 -> "001", з 12 -> "012"
      return 'SC-' + scene.sequenceNum.toString().padStart(3, '0');
    }
    return 'SC-000';
  }

  // 2. Метод створення нової сцени
  createNewScene() {
    if (!this.canEdit) return;

    this.api.createScene(this.projectId).subscribe({
      next: (newScene) => {
        this.scenes.push(newScene); // Додаємо в лівий список
        this.selectScene(newScene.id); // Одразу відкриваємо її для редагування
      },
      error: (err) => console.error('Failed to create scene', err)
    });
  }

  // --- ЛОГІКА ПЕРЕТВОРЕННЯ (WRITE MODE) ---
  buildRawTextFromBlocks() {
    this.rawScriptText = this.blocks.map(b => b.content).join('\n\n');
  }

  parseRawTextToBlocks() {
    // Розділяємо текст по подвійному Enter
    const paragraphs = this.rawScriptText.split(/\n\s*\n/).filter(p => p.trim() !== '');

    // Робимо копію старих блоків для "розумного" пошуку
    let oldBlocks = [...this.blocks];

    this.blocks = paragraphs.map((text) => {
      let t = text.trim();

      // ШУКАЄМО блок за його текстом, а не за номером рядка!
      const matchIndex = oldBlocks.findIndex(b => b.content === t);
      let existing;

      if (matchIndex !== -1) {
        existing = oldBlocks[matchIndex];
        // Видаляємо знайдений блок зі списку пошуку, щоб не прив'язати його двічі
        oldBlocks.splice(matchIndex, 1);
      }

      let autoType: BlockType = 'action';
      if (t.startsWith('INT.') || t.startsWith('EXT.')) autoType = 'scene_heading';
      else if (t.startsWith('(') && t.endsWith(')')) autoType = 'parenthetical';
      else if (t.endsWith('TO:') || t.endsWith('IN:')) autoType = 'transition';

      return {
        id: existing?.id || 'b-' + Math.random().toString(36).substring(2, 9),
        type: existing ? existing.type : autoType, // Якщо знайшли старий текст - зберігаємо його тег!
        content: t,
        color: existing?.color || '#444444',
        sceneId: existing?.sceneId || this.sceneId,
        charName: existing?.charName
      };
    });

    this.updateColors();
    this.syncSidebarTitle();
  }

  // --- DRAG AND DROP ЛОГІКА ---
  dropScene(event: CdkDragDrop<any[]>) {
    // Якщо користувач не має прав на редагування, забороняємо переміщення
    if (!this.canEdit) return;

    // Переміщуємо елемент у локальному масиві
    moveItemInArray(this.scenes, event.previousIndex, event.currentIndex);

    // Миттєво оновлюємо візуальні номери (SequenceNum) для всіх сцен
    this.scenes.forEach((scene, index) => {
      scene.sequenceNum = index + 1;
    });

    // Збираємо новий порядок ID і відправляємо на сервер
    const orderedIds = this.scenes.map(s => s.id);
    this.api.reorderScenes(this.projectId, orderedIds).subscribe({
      error: (err) => console.error('Failed to reorder scenes', err)
    });
  }

  // --- НОВИЙ МЕТОД ДЛЯ МИТТЄВОГО ОНОВЛЕННЯ КОЛЬОРІВ ---
  updateColors() {
    // КРОК 1: Оновлюємо кольори самих персонажів з бази проекту
    for (const block of this.blocks) {
      if (block.type === 'character') {
        const charName = block.content.trim().toUpperCase();
        const projectRole = this.availableResources.roles.find(r => r.name.toUpperCase() === charName);

        block.color = projectRole ? projectRole.color : (block.color !== '#444444' ? block.color : '#3AB9A0');
        block.charName = charName;
      }
    }

    // КРОК 2: Жорстко прив'язуємо кожну репліку до її персонажа
    for (let i = 0; i < this.blocks.length; i++) {
      const block = this.blocks[i];

      if (block.type === 'dialogue' || block.type === 'parenthetical') {
        const owner = this.getNearestCharacter(i); // Знаходимо, чия це репліка
        if (owner) {
          block.color = owner.color;
          block.charName = owner.name; // Завдяки цьому працює підсвітка!
        } else {
          block.color = '#444444';
          block.charName = undefined;
        }
      } else if (block.type !== 'character') {
        block.color = '#444444';
        block.charName = undefined;
      }
    }

    // КРОК 2: Жорстко прив'язуємо кожну репліку до її персонажа
    for (let i = 0; i < this.blocks.length; i++) {
      const block = this.blocks[i];

      if (block.type === 'dialogue' || block.type === 'parenthetical') {
        const owner = this.getNearestCharacter(i); // Знаходимо, чия це репліка
        if (owner) {
          block.color = owner.color;
          block.charName = owner.name; // Завдяки цьому працює підсвітка!
        } else {
          block.color = '#444444';
          block.charName = undefined;
        }
      } else if (block.type !== 'character') {
        block.color = '#444444';
        block.charName = undefined;
      }
    }
  }


  // --- ДОПОМІЖНИЙ МЕТОД: Шукає власника репліки ---
  private getNearestCharacter(currentIndex: number): { name: string, color: string } | null {
    // Скануємо блоки знизу вгору від поточної репліки
    for (let i = currentIndex - 1; i >= 0; i--) {
      if (this.blocks[i].type === 'character') {
        const name = this.blocks[i].content.trim().toUpperCase();

        // Витягуємо колір у змінну. Якщо він є і він не сірий - беремо його, інакше дефолтний
        const blockColor = this.blocks[i].color;
        const color = (blockColor && blockColor !== '#444444') ? blockColor : '#3AB9A0';

        return { name, color };
      }
    }
    return null; // Якщо зверху взагалі немає персонажів
  }

  // --- ЛОГІКА UI БЛОКІВ ---
  toggleTypeMenu(blockId: string) {
    this.openTypeMenuId = this.openTypeMenuId === blockId ? null : blockId;
  }

  setBlockType(block: ScriptBlock, newType: BlockType) {
    block.type = newType;
    this.openTypeMenuId = null;
    this.updateColors();
    this.syncSidebarTitle();

    // НОВЕ: Рахуємо, скільки заголовків стало у сцені після зміни типу
    const headingsCount = this.blocks.filter(b => b.type === 'scene_heading').length;

    if (headingsCount > 1) {
      // Якщо ми створили другий заголовок — примусово відправляємо на бекенд,
      // щоб він миттєво розрізав сцену і перевів нас у Full Script
      this.performManualSave();
    } else {
      // Інакше — просто ставимо в чергу на звичайне автозбереження
      this.autosaveSubject.next();
    }
  }

  // Вибір класу лінії в залежності від типу блоку
  getBlockLineClass(block: ScriptBlock): string {
    if (this.viewMode === 'read' && !this.showLinesInRead) return 'line-none';

    switch (block.type) {
      case 'character': return 'line-solid';
      case 'dialogue': return 'line-wavy';         // Хвилька
      case 'parenthetical': return 'line-dashed';  // Пунктир
      case 'transition': return 'line-zigzag';     // Зигзаг
      case 'scene_heading': return 'line-double';  // Подвійна
      case 'shot': return 'line-dotted';           // Крапочки
      case 'action': default: return 'line-none';  // Без лінії
    }
  }

  getTypeIcon(type: BlockType): string {
    switch(type) {
      case 'scene_heading': return '🎬';
      case 'action': return '📝';
      case 'character': return '👤';
      case 'dialogue': return '💬';
      case 'parenthetical': return '()';
      case 'transition': return '✂️';
      case 'shot': return '🎥';
      default: return '📄';
    }
  }

  // --- УПРАВЛІННЯ РЕСУРСАМИ (ПРАВА ПАНЕЛЬ) ---
  openResourceModal(type: 'role' | 'location' | 'prop') {
    if (!this.canEdit) return;
    this.resourceModalType = type;
    this.resourceSearch = '';
    this.isResourceModalOpen = true;
  }

  getFilteredResources() {
    const q = this.resourceSearch.toLowerCase().trim();
    let list: any[] = [];

    if (this.resourceModalType === 'role') {
      // Показуємо тільки тих, кого ЩЕ НЕМАЄ в активних персонажах сцени
      list = this.availableResources.roles.filter(r =>
        !this.activeCharacters.some(ac => ac.id === r.id)
      );
    } else if (this.resourceModalType === 'location') {
      list = this.availableResources.locations.filter(l => !this.linkedLocations.some(ll => ll.id === l.id));
    } else {
      list = this.availableResources.props.filter(p => !this.linkedProps.some(lp => lp.id === p.id));
    }

    return list.filter(item => item.name.toLowerCase().includes(q));
  }

  linkResource(resourceId: number) {
    this.api.linkResource(this.sceneId, resourceId).subscribe({
      next: () => {
        this.isResourceModalOpen = false;

        if (this.resourceModalType === 'role') {
          // Одразу додаємо роль у праву панель без перезавантаження всього скрипту
          const role = this.availableResources.roles.find(r => r.id === resourceId);
          if (role && !this.activeCharacters.some(ac => ac.id === role.id)) {
            this.activeCharacters = [...this.activeCharacters, {
              id: role.id,
              name: role.name,
              color: role.color,
              initials: role.name.charAt(0),
              isAuto: false
            }];
          }
          this.cdr.detectChanges();
        } else {
          // Для локацій та пропів — перезавантажуємо як раніше
          this.loadData();
        }
      }
    });
  }

  quickCreateResource() {
    this.api.quickCreateResource(this.projectId, this.resourceModalType, this.resourceSearch.trim()).subscribe({
      next: (newRes) => {
        // ... (ваш існуючий код додавання)
        if (this.resourceModalType === 'role') this.availableResources.roles.push(newRes);
        if (this.resourceModalType === 'location') this.availableResources.locations.push(newRes);
        if (this.resourceModalType === 'prop') this.availableResources.props.push(newRes);

        this.linkResource(newRes.id);
      },
      error: (err) => {
        alert(err.error?.message || 'Error creating resource.');
      }
    });
  }

  unlinkResource(type: string, resourceId: number) {
    if (!this.canEdit) return;
    this.api.unlinkResource(this.sceneId, resourceId).subscribe({
      next: () => this.loadData()
    });
  }

  // ДОДАЙТЕ ЦЕЙ МЕТОД: Обробка події з HTML Color Picker
  onColorChange(roleId: number, event: Event) {
    const input = event.target as HTMLInputElement;
    this.changeRoleColor(roleId, input.value);
  }

  changeRoleColor(roleId: number, colorHex: string) {
    if (!roleId) return;
    this.api.updateRoleColor(roleId, colorHex).subscribe({
      next: () => {
        // Оновлюємо локально
        const r = this.availableResources.roles.find(x => x.id === roleId);
        if (r) r.color = colorHex;

        this.openCharMenuId = null;
        this.loadData(); // Перемалювати скрипт і панель з новим кольором
      }
    });
  }

  // ДОДАЙТЕ ЦІ ДВІ ЗМІННІ
  menuTop: number = 0;
  menuRight: number = 0;

  // ОНОВІТЬ ЦЕЙ МЕТОД
  toggleCharMenu(event: MouseEvent, id: string) {
    if (this.openCharMenuId === id) {
      this.openCharMenuId = null;
    } else {
      this.openCharMenuId = id;

      // Беремо координати кнопки "...", на яку щойно клікнули
      const target = event.currentTarget as HTMLElement;
      const rect = target.getBoundingClientRect();

      // Виставляємо меню рівно по висоті кнопки та трохи лівіше від неї
      this.menuTop = rect.top;
      this.menuRight = window.innerWidth - rect.left + 10;
    }
  }

  goToCasting(roleId: number) {
    // Перехід на вкладку кастингу з передачею ID ролі
    this.router.navigate(['../casting'], {
      relativeTo: this.route,
      queryParams: { roleId: roleId }
    });
  }

  findMyApprovedRole() {
    const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
    if (!user.id || !this.projectId) return;

    this.api.getActorCastingsInProject(this.projectId, user.id).subscribe(castings => {
      // ✅ Збираємо ВСІ затверджені ролі (може бути кілька)
      this.myApprovedRoleIds = new Set(
        castings
          .filter((c: any) => c.status === 'approved' || c.castStatus === 'approved')
          .map((c: any) => c.roleId)
      );
      this.cdr.detectChanges();
    });
  }
}
