import { Component, ElementRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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
  imports: [CommonModule, FormsModule],
  templateUrl: './script.html',
  styleUrl: './script.scss'
})
export class Script implements OnInit {
  @ViewChild('scriptPaper') scriptPaper!: ElementRef;

  // --- СТАНИ UI ---
  isLeftOpen = true;
  isRightOpen = true;
  viewMode: ViewMode = 'breakdown'; // Режими: edit, breakdown, read
  sceneViewMode: SceneViewMode = 'single';

  // Додаткові стани
  showLinesInRead = true; // Перемикач ліній у режимі читання
  rawScriptText = '';     // Текст для Write (edit) режиму
  openTypeMenuId: string | null = null;

  @ViewChild('rawEditor') rawEditorRef!: ElementRef;

  ngOnInit() {
    // Якщо починаємо з режиму редагування, генеруємо текст
    if (this.viewMode === 'edit') {
      this.buildRawTextFromBlocks();
    }
  }

  toggleLeft() { this.isLeftOpen = !this.isLeftOpen; }
  toggleRight() { this.isRightOpen = !this.isRightOpen; }

  onRawInput(event: Event) {
    const el = event.target as HTMLElement;
    // textContent не додає зайвих \n від div/br
    this.rawScriptText = el.innerText
      .replace(/\n{3,}/g, '\n\n'); // не більше одного порожнього рядка
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

  // --- ДАНІ ---
  scenes = [
    { id: 'SC-001', heading: 'INT. CAFE - DAY', pages: '1.5 p.', time: '00:05:00', isActive: false },
    { id: 'SC-002', heading: 'EXT. NIGHT STREET', pages: '0.75 p.', time: '00:03:00', isActive: true },
    { id: 'SC-003', heading: 'INT. SARAH\'S HOUSE', pages: '0.5 p.', time: '00:02:00', isActive: false }
  ];

  activeCharacters = [
    { id: 'char-1', initials: 'J', name: 'JOHN', type: 'Lead', color: '#3AB9A0' },
    { id: 'char-2', initials: 'S', name: 'SARAH', type: 'Lead', color: '#E9A60F' },
    { id: 'char-3', initials: 'D', name: 'DETECTIVE CHEN', type: 'Lead', color: '#8B5CF6' }
  ];

  blocks: ScriptBlock[] = [
    { id: 'b1', type: 'scene_heading', content: 'EXT. NIGHT STREET' },
    { id: 'b2', type: 'action', content: 'Джон виходить з кафе. Вулиця покрита дощем, відблиски вогнів у калюжах.' },
    { id: 'b3', type: 'action', content: 'Він дістає телефон, набирає номер. Довгі гудки.' },
    { id: 'b4', type: 'character', content: 'JOHN', charId: 'char-1', color: '#3AB9A0' },
    { id: 'b5', type: 'parenthetical', content: 'у телефон', color: '#3AB9A0' },
    { id: 'b6', type: 'dialogue', content: 'Нам потрібно поговорити. Сьогодні ввечері.', color: '#3AB9A0' },
    { id: 'b7', type: 'action', content: 'Він кидає недопалок на землю і йде нічною вулицею.' },
    { id: 'b8', type: 'character', content: 'SARAH', charId: 'char-2', color: '#E9A60F' },
    { id: 'b9', type: 'dialogue', content: 'Джон? Я тебе погано чую, зв\'язок переривається.', color: '#E9A60F' },
    { id: 'b10', type: 'transition', content: 'CUT TO:' },
    { id: 'b11', type: 'shot', content: 'CLOSE UP ON PHONE' }
  ];

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

  // --- ЛОГІКА UI БЛОКІВ ---
  toggleTypeMenu(blockId: string) {
    this.openTypeMenuId = this.openTypeMenuId === blockId ? null : blockId;
  }

  setBlockType(block: ScriptBlock, newType: BlockType) {
    block.type = newType;
    this.openTypeMenuId = null;
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
