import { Component, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
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
export class Script implements AfterViewChecked {
  @ViewChild('scriptPaper') scriptPaper!: ElementRef;

  // --- СТАНИ UI ---
  isLeftOpen = true;
  isRightOpen = true;
  viewMode: ViewMode = 'breakdown'; // Режими: edit, breakdown, read
  sceneViewMode: SceneViewMode = 'single'; // Режими перегляду: Одна сцена або Всі

  // Стан для відкритого меню типу блоку
  openTypeMenuId: string | null = null;

  toggleLeft() { this.isLeftOpen = !this.isLeftOpen; }
  toggleRight() { this.isRightOpen = !this.isRightOpen; }

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

  // Сценарій
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
    { id: 'b10', type: 'transition', content: 'CUT TO:' }
  ];

  // --- ЛОГІКА ---

  // Авто-висота для textarea в режимі Edit
  ngAfterViewChecked() {
    if (this.viewMode === 'edit') {
      const textareas = this.scriptPaper?.nativeElement.querySelectorAll('textarea');
      textareas?.forEach((ta: HTMLTextAreaElement) => {
        ta.style.height = 'auto';
        ta.style.height = ta.scrollHeight + 'px';
      });
    }
  }

  onInputResize(event: any) {
    event.target.style.height = 'auto';
    event.target.style.height = event.target.scrollHeight + 'px';
  }

  // Керування меню типів
  toggleTypeMenu(blockId: string) {
    this.openTypeMenuId = this.openTypeMenuId === blockId ? null : blockId;
  }

  setBlockType(block: ScriptBlock, newType: BlockType) {
    block.type = newType;
    this.openTypeMenuId = null; // Закрити меню
  }

  // Магія стилів (Лінії строго збоку)
  getBlockStyle(block: ScriptBlock) {
    if (this.viewMode === 'read') return {}; // В режимі читання ліній немає

    const color = block.color || '#444'; // Якщо немає кольору, ставимо сірий

    switch (block.type) {
      case 'character':
      case 'dialogue':
        return { 'border-left-color': color, 'border-left-width': '4px', 'border-left-style': 'solid' };
      case 'parenthetical':
        return { 'border-left-color': color, 'border-left-width': '4px', 'border-left-style': 'dashed' };
      case 'action':
        return { 'border-left-color': 'transparent' };
      case 'scene_heading':
        return { 'border-left-color': '#fff', 'border-left-width': '4px', 'border-left-style': 'solid' };
      case 'transition':
        return { 'border-right-color': '#fff', 'border-right-width': '4px', 'border-right-style': 'dotted', 'border-left-color': 'transparent' };
      default:
        return {};
    }
  }

  // Отримання іконки для меню
  getTypeIcon(type: BlockType): string {
    switch(type) {
      case 'scene_heading': return '🎬';
      case 'action': return '📝';
      case 'character': return '👤';
      case 'dialogue': return '💬';
      case 'parenthetical': return '()';
      case 'transition': return '✂️';
      default: return '📄';
    }
  }
}
