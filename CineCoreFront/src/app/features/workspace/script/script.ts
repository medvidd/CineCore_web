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
    if (this.sceneId === id) return; // Не перезавантажувати, якщо вже обрана

    // Якщо ми в режимі 'edit', перед зміною сцени зберігаємо поточну
    if (this.viewMode === 'edit') {
      this.parseRawTextToBlocks();
      this.performAutosave();
    }

    this.sceneId = id;
    this.loadSceneScript();
  }

  loadSceneScript() {
    this.api.getSceneScript(this.sceneId).subscribe({
      next: (data: any) => {
        this.blocks = data.blocks;       // Беремо блоки з нового об'єкта
        this.sceneNotes = data.notes || ''; // Беремо нотатки

        this.extractCharactersFromBlocks();

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
    if (!this.canEdit) return; // Якщо актор - не зберігаємо

    this.api.autoSaveScript(this.sceneId, this.projectId, this.blocks).subscribe({
      next: () => {
        console.log('Script autosaved!');
        this.extractCharactersFromBlocks(); // Оновлюємо список ролей збоку
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

  setViewMode(mode: ViewMode) {
    if (this.viewMode === 'edit' && mode !== 'edit') {
      // Читаємо актуальний текст з DOM перед переключенням
      if (this.rawEditorRef?.nativeElement) {
        this.rawScriptText = this.rawEditorRef.nativeElement.innerText
          .replace(/\n{3,}/g, '\n\n');
      }
      this.parseRawTextToBlocks();
    } else if (this.viewMode !== 'edit' && mode === 'edit') {
      this.buildRawTextFromBlocks();
    }

    this.viewMode = mode;

    if (mode === 'edit') {
      setTimeout(() => {
        if (this.rawEditorRef?.nativeElement) {
          const el = this.rawEditorRef.nativeElement;
          // Очищаємо HTML повністю і пишемо plain text
          el.textContent = this.rawScriptText;
        }
      }, 0);
    }
  }

  // Функція, яка робить textarea "гумовою"
  autoResize(textarea: HTMLTextAreaElement | null) {
    if (!textarea) return;
    textarea.style.height = 'auto'; // Важливо спочатку скинути
    textarea.style.height = (textarea.scrollHeight) + 'px'; // Встановити висоту контенту
  }


  // blocks: ScriptBlock[] = [
  //   { id: 'b1', type: 'scene_heading', content: 'EXT. NIGHT STREET' },
  //   { id: 'b2', type: 'action', content: 'Джон виходить з кафе. Вулиця покрита дощем, відблиски вогнів у калюжах.' },
  //   { id: 'b3', type: 'action', content: 'Він дістає телефон, набирає номер. Довгі гудки.' },
  //   { id: 'b4', type: 'character', content: 'JOHN', charId: 'char-1', color: '#3AB9A0' },
  //   { id: 'b5', type: 'parenthetical', content: 'у телефон', color: '#3AB9A0' },
  //   { id: 'b6', type: 'dialogue', content: 'Нам потрібно поговорити. Сьогодні ввечері.', color: '#3AB9A0' },
  //   { id: 'b7', type: 'action', content: 'Він кидає недопалок на землю і йде нічною вулицею.' },
  //   { id: 'b8', type: 'character', content: 'SARAH', charId: 'char-2', color: '#E9A60F' },
  //   { id: 'b9', type: 'dialogue', content: 'Джон? Я тебе погано чую, зв\'язок переривається.', color: '#E9A60F' },
  //   { id: 'b10', type: 'transition', content: 'CUT TO:' },
  //   { id: 'b11', type: 'shot', content: 'CLOSE UP ON PHONE' }
  // ];

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

      // Простенька авторозмітка для зручності
      if (t.startsWith('INT.') || t.startsWith('EXT.')) autoType = 'scene_heading';
      else if (t.startsWith('(') && t.endsWith(')')) autoType = 'parenthetical';
      else if (t === t.toUpperCase() && t.length < 35 && !t.includes('CUT')) autoType = 'character';
      else if (t.endsWith('TO:') || t.endsWith('IN:')) autoType = 'transition';

      return {
        id: existing?.id || 'b-' + Math.random().toString(36).substring(2, 9),
        type: (existing && existing.content === t) ? existing.type : autoType,
        content: t,
        color: existing?.color || '#444'
      };
    });
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

  // --- ЛОГІКА UI БЛОКІВ ---
  toggleTypeMenu(blockId: string) {
    this.openTypeMenuId = this.openTypeMenuId === blockId ? null : blockId;
  }

  setBlockType(block: ScriptBlock, newType: BlockType) {
    block.type = newType;
    this.openTypeMenuId = null;
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
