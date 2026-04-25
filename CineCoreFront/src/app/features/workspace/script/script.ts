import { Component, ElementRef, ViewChild, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';
import { Subject} from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { ActivatedRoute } from '@angular/router';
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

  projectId: number = 0;
  sceneId: number = 0;
  sceneNotes: string = '';

  private autosaveSubject = new Subject<void>();
  private notesAutosaveSubject = new Subject<string>();

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

  sceneLocations: any[] = [];
  sceneProps: any[] = [];

  ngOnInit() {
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    // 1. Отримуємо ID проекту та сцени (припустимо, вони є в URL)
    this.route.parent?.paramMap.subscribe(params => {
      const pid = params.get('id');
      if (pid) {
        this.projectId = Number(pid);
        this.loadScenesList(); // Завантажуємо ліву панель!
      }
    });

    // Для тесту можемо взяти першу сцену з вашого хардкодного списку
    this.route.parent?.paramMap.subscribe(params => {
      const pid = params.get('id');
      if (pid) {
        this.projectId = Number(pid);
        this.loadScenesList(); // Завантажуємо ліву панель!
      }
    });

    // 2. НАЛАШТУВАННЯ АВТОЗБЕРЕЖЕННЯ (чекає 3 секунди після зупинки вводу)
    this.autosaveSubject.pipe(debounceTime(3000)).subscribe(() => {
      this.performAutosave();
    });

    this.notesAutosaveSubject.pipe(debounceTime(2000)).subscribe((notes) => {
      this.api.updateSceneNotes(this.sceneId, notes).subscribe();
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

  selectScene(id: number) {
    if (this.sceneViewMode === 'all') {
      this.sceneId = id; // Тільки підсвічуємо
      this.scrollToScene(id); // Скролимо
      return;
    }

    if (this.canEdit && this.sceneId !== 0) {
      this.performManualSave();
    }

    this.sceneId = id;
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

  performManualSave() {
    if (this.viewMode === 'edit') {
      this.parseRawTextToBlocks();
    }
    // Відправляємо запит без debounce
    this.api.autoSaveScript(this.sceneId, this.projectId, this.blocks).subscribe();
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
        this.blocks = data; // GetFullScript повертає просто масив блоків
        this.extractCharactersFromBlocks();
        this.updateColors(); // Одразу розфарбовуємо всіх персонажів
        this.cdr.detectChanges();
      }
    });
  }

  setViewMode(mode: ViewMode) {
    if (this.viewMode === 'edit') {
      this.parseRawTextToBlocks();
      this.performAutosave(); // Зберігаємо в БД перед зміною режиму
    }

    this.viewMode = mode;

    if (mode === 'edit') {
      // Якщо ми були в режимі Full Script і натиснули Write,
      // треба автоматично перемкнутися на Single Scene
      if (this.sceneViewMode === 'all') {
        this.sceneViewMode = 'single';
        this.api.getSceneScript(this.sceneId).subscribe({
          next: (data: any) => {
            this.blocks = data.blocks;
            this.sceneNotes = data.notes || '';
            this.extractCharactersFromBlocks();
            this.updateColors();
            this.buildRawTextFromBlocks();
            setTimeout(() => {
              if (this.rawEditorRef?.nativeElement) {
                this.rawEditorRef.nativeElement.innerText = this.rawScriptText;
              }
            }, 0);
          }
        });
      } else {
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

    this.api.autoSaveScript(this.sceneId, this.projectId, this.blocks).subscribe({
      next: () => {
        console.log('Script autosaved!');
        this.extractCharactersFromBlocks();
      },
      error: (err) => console.error('Autosave failed', err)
    });
  }

  extractCharactersFromBlocks() {
    const charNames = [...new Set(this.blocks
      .filter(b => b.type === 'character')
      .map(b => b.content.trim().toUpperCase())
    )];

    this.activeCharacters = charNames.map((name, i) => ({
      id: 'char-' + i,
      initials: name.charAt(0),
      name: name,
      type: 'Auto',
      color: '#3AB9A0' // Можна брати з БД, якщо повертати в DTO
    }));
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

    this.blocks = paragraphs.map((text, i) => {
      const existing = this.blocks[i];
      let t = text.trim();
      let autoType: BlockType = 'action';

      // Простенька авторозмітка (ЗАБРАЛИ РЯДОК З CHARACTER)
      if (t.startsWith('INT.') || t.startsWith('EXT.')) autoType = 'scene_heading';
      else if (t.startsWith('(') && t.endsWith(')')) autoType = 'parenthetical';
      else if (t.endsWith('TO:') || t.endsWith('IN:')) autoType = 'transition';

      return {
        id: existing?.id || 'b-' + Math.random().toString(36).substring(2, 9),
        type: (existing && existing.content === t) ? existing.type : autoType,
        content: t,
        color: existing?.color || '#444',
        sceneId: existing?.sceneId || this.sceneId
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
    let currentColor = '#444'; // Сірий за замовчуванням

    for (const block of this.blocks) {
      if (block.type === 'character') {
        const charName = block.content.trim().toUpperCase();
        // Шукаємо, чи є вже такий персонаж із завантаженим кольором з БД
        const existingColor = this.blocks.find(b => b.type === 'character' && b.content.trim().toUpperCase() === charName && b.color && b.color !== '#444')?.color;

        currentColor = existingColor || '#3AB9A0'; // Використовуємо реальний колір або дефолтний бірюзовий
        block.color = currentColor;
      }
      else if (block.type === 'dialogue' || block.type === 'parenthetical') {
        // Ремарки та діалоги УСПАДКОВУЮТЬ колір останнього знайденого персонажа
        block.color = currentColor;
      }
      else {
        // Дії, переходи, шоти - ЗАВЖДИ СІРІ
        block.color = '#444';
      }
    }
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
    this.autosaveSubject.next();
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

}
