import xml.etree.ElementTree as ET
import openpyxl
from openpyxl.styles import Font
from deep_translator import GoogleTranslator
import re
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
import threading
import socket
import datetime
import os
from typing import List, Dict, Tuple

class FGUITranslatorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("FGUI多语言翻译工具 v2.0 - Excel模式")
        self.root.geometry("800x600")
        
        # 初始化所有属性
        self.supported_languages = {
            #'zh-TW': '繁体中文',
            #'zh-CN': '简体中文',
            'es': '西班牙',
            'fr': '法国',
            #'ja': '日本',
            #'de': '德国',
            #'ru': '俄罗斯',
            'pt': '葡萄牙',
            #'hi': '印地语',
            'vi': '越南',
            'tr': '土耳其',
            'id': '印度尼西亚',
            'tl': '菲律宾语',
            #'ar': '阿拉伯',
            #'pl': '波兰',
            #'th': '泰国',
            #'ko': '韩国',
            #'uk': '乌克兰',
            #'ro': '罗马尼亚',
            #'ms': '马来西亚'

        }
        
        self.lang_code_mapping = {
            'zh': 'zh-TW',
            'zh_CN': 'zh-CN',
            '@in': 'id',
            'hi_IN': 'hi'
        }
        
        # 创建界面组件
        self.create_widgets()
        
        # 检查网络
        self.network_available = True
        #self.check_network()

    def check_network(self):
        """检测网络连接"""
        test_urls = [
        ("www.google.com", 80),    # HTTP
        ("www.google.com", 443),   # HTTPS
        ("8.8.8.8", 53),           # Google DNS
        ("1.1.1.1", 53),           # Cloudflare DNS
        ("www.baidu.com", 80),     # 备用检测
        ("www.microsoft.com", 80)  # 备用检测
        ]
    
        self.network_available = False
    
        for host, port in test_urls:
            try:
                socket.create_connection((host, port), timeout=5)
                self.network_available = True
                self.log(f"网络检测: 成功连接到 {host}:{port}")
                break
            except socket.timeout:
                self.log(f"网络检测: 连接 {host}:{port} 超时", "warning")
            except OSError as e:
                self.log(f"网络检测: 无法连接 {host}:{port} ({str(e)})", "warning")

    def create_widgets(self):
        """创建GUI组件"""
        # 顶部框架
        top_frame = tk.Frame(self.root, padx=10, pady=10)
        top_frame.pack(fill=tk.X)
        
        # 输入Excel文件选择
        tk.Label(top_frame, text="输入Excel文件:").grid(row=0, column=0, sticky=tk.W)
        self.input_entry = tk.Entry(top_frame, width=50)
        self.input_entry.grid(row=0, column=1, padx=5)
        tk.Button(top_frame, text="浏览...", command=self.browse_input_file).grid(row=0, column=2)
        
        # 输出文件设置
        tk.Label(top_frame, text="输出Excel文件:").grid(row=1, column=0, sticky=tk.W)
        self.output_entry = tk.Entry(top_frame, width=50)
        self.output_entry.grid(row=1, column=1, padx=5)
        tk.Button(top_frame, text="浏览...", command=self.browse_output_file).grid(row=1, column=2)
        
        # 语言选择
        lang_frame = tk.LabelFrame(self.root, text="选择目标语言", padx=10, pady=10)
        lang_frame.pack(fill=tk.X, padx=10, pady=5)
        
        self.lang_vars = {}
        for i, (code, name) in enumerate(self.supported_languages.items()):
            var = tk.BooleanVar(value=True)
            self.lang_vars[code] = var
            
            cb = tk.Checkbutton(lang_frame, text=name, variable=var)
            cb.grid(row=i//4, column=i%4, sticky=tk.W, padx=5, pady=2)
        
        # 操作按钮
        btn_frame = tk.Frame(self.root, pady=10)
        btn_frame.pack(fill=tk.X)
        
        tk.Button(btn_frame, text="开始翻译", command=self.start_translation, 
                 bg="#4CAF50", fg="white").pack(side=tk.LEFT, padx=10)
        tk.Button(btn_frame, text="清空日志", command=self.clear_log).pack(side=tk.LEFT)
        tk.Button(btn_frame, text="退出", command=self.root.quit).pack(side=tk.RIGHT, padx=10)
        
        # 进度条
        self.progress = ttk.Progressbar(self.root, orient=tk.HORIZONTAL, mode='determinate')
        self.progress.pack(fill=tk.X, padx=10, pady=5)
        
        # 日志区域
        log_frame = tk.LabelFrame(self.root, text="操作日志", padx=10, pady=10)
        log_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        self.log_text = tk.Text(log_frame, height=10, wrap=tk.WORD)
        self.log_text.pack(fill=tk.BOTH, expand=True)
        
        scrollbar = tk.Scrollbar(self.log_text)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.log_text.config(yscrollcommand=scrollbar.set)
        scrollbar.config(command=self.log_text.yview)
        
        # 初始化日志
        self.log("FGUI多语言翻译工具已启动 (Excel模式)")
        self.log(f"当前时间: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    def browse_input_file(self):
        """选择输入Excel文件"""
        filepath = filedialog.askopenfilename(
            title="选择输入Excel文件",
            filetypes=[("Excel文件", "*.xlsx"), ("所有文件", "*.*")]
        )
        if filepath:
            self.input_entry.delete(0, tk.END)
            self.input_entry.insert(0, filepath)
            
            # 自动生成输出文件名
            dirname, filename = os.path.split(filepath)
            basename = os.path.splitext(filename)[0]
            output_path = os.path.join(dirname, f"{basename}_translated.xlsx")
            self.output_entry.delete(0, tk.END)
            self.output_entry.insert(0, output_path)
            
            self.log(f"已选择输入文件: {filepath}")

    def browse_output_file(self):
        """选择输出Excel文件"""
        filepath = filedialog.asksaveasfilename(
            title="保存翻译结果",
            defaultextension=".xlsx",
            filetypes=[("Excel文件", "*.xlsx"), ("所有文件", "*.*")]
        )
        if filepath:
            self.output_entry.delete(0, tk.END)
            self.output_entry.insert(0, filepath)
            self.log(f"已设置输出文件: {filepath}")

    def start_translation(self):
        """开始翻译"""
        if not self.network_available:
            messagebox.showerror("错误", "网络不可用，请检查网络连接!")
            return
            
        input_file = self.input_entry.get()
        output_file = self.output_entry.get()
        
        if not input_file or not output_file:
            messagebox.showerror("错误", "请先选择输入和输出文件!")
            return
            
        # 获取选中的目标语言
        selected_langs = {code: name for code, name in self.supported_languages.items() 
                         if code in self.lang_vars and self.lang_vars[code].get()}
        
        if not selected_langs:
            messagebox.showerror("错误", "请至少选择一种目标语言!")
            return
            
        # 禁用按钮防止重复点击
        for widget in self.root.winfo_children():
            if isinstance(widget, tk.Button):
                widget.config(state=tk.DISABLED)
        
        # 在新线程中执行翻译
        self.log("\n开始翻译任务...")
        self.log(f"输入文件: {input_file}")
        self.log(f"输出文件: {output_file}")
        self.log(f"目标语言: {', '.join(selected_langs.values())}")
        
        thread = threading.Thread(
            target=self.do_translation,
            args=(input_file, output_file, selected_langs),
            daemon=True
        )
        thread.start()

    def do_translation(self, input_file, output_file, selected_langs):
        """执行翻译任务"""
        try:
            # 读取Excel文件
            self.log("正在读取Excel文件...")
            texts = self.read_texts_from_excel(input_file)
            if not texts:
                self.log("错误: 没有读取到任何文本!", "error")
                return
                
            self.log(f"共读取到 {len(texts)} 条文本")
            
            # 创建新的Excel工作簿
            wb = openpyxl.Workbook()
            ws = wb.active
            ws.title = "TextLanguage"
            
            # 设置列宽
            ws.column_dimensions['A'].width = 8
            ws.column_dimensions['B'].width = 30
            ws.column_dimensions['C'].width = 20
            for col in range(4, 4 + len(selected_langs)):
                ws.column_dimensions[openpyxl.utils.get_column_letter(col)].width = 20
            
            # 写入表头
            headers = ['id', 'key', 'mz', 'en'] + list(selected_langs.keys())
            ws.append(headers)
            
            # 写入数据类型行
            ws.append(['int'] + ['string'] * (len(headers) - 1))
            
            # 写入标题行
            title_row = ['id', '组件索引', '控件名字', '英文'] + list(selected_langs.values())
            ws.append(title_row)
            
            # 写入语言标记行
            ws.append(['L'] * len(headers))
            
            # 更新进度条
            total_texts = len(texts)
            self.progress["maximum"] = total_texts
            
            # 逐条翻译
            for i, text_item in enumerate(texts, 1):
                row = [text_item['id'], text_item['key'], text_item['mz'], text_item['text']]
                
                for lang_code in selected_langs.keys():
                    translated = self.translate_text(
                        text_item['text'],
                        'en',  # 源语言是英文
                        lang_code
                    )
                    row.append(translated)
                
                ws.append(row)
                
                # 更新进度
                self.progress["value"] = i
                self.root.update()
                
                if i % 10 == 0 or i == total_texts:
                    self.log(f"已翻译 {i}/{total_texts} 条 ({i/total_texts:.1%})")
            
            # 设置表头样式
            header_font = Font(bold=True)
            for row in ws.iter_rows(min_row=1, max_row=4):
                for cell in row:
                    cell.font = header_font
            
            # 保存文件
            wb.save(output_file)
            self.log(f"\n翻译完成! 结果已保存到: {output_file}", "success")
            
        except Exception as e:
            self.log(f"翻译过程中发生错误: {str(e)}", "error")
        finally:
            # 重新启用按钮
            self.root.after(0, self.enable_buttons)
            self.progress["value"] = 0

    def read_texts_from_excel(self, excel_file: str) -> List[Dict[str, str]]:
        """从Excel文件中读取文本数据"""
        try:
            wb = openpyxl.load_workbook(excel_file)
            ws = wb.active
            
            texts = []
            
            # 从第5行开始读取数据（跳过表头、数据类型、标题行、语言标记行）
            for row in ws.iter_rows(min_row=5, values_only=True):
                if not row[0] or not row[3]:  # 跳过空行或没有英文文本的行
                    continue
                    
                text_id = row[0]
                key = row[1] or ''
                mz = row[2] or ''
                english_text = row[3] or ''
                
                texts.append({
                    'id': text_id,
                    'key': key,
                    'mz': mz,
                    'text': english_text,
                    'original_lang': 'en'
                })
            
            return texts
        except Exception as e:
            self.log(f"读取Excel文件错误: {str(e)}", "error")
            return []

    def enable_buttons(self):
        """重新启用所有按钮"""
        for widget in self.root.winfo_children():
            if isinstance(widget, tk.Button):
                widget.config(state=tk.NORMAL)

    def extract_text_from_xml(self, xml_file: str) -> List[Dict[str, str]]:
        """从XML文件中提取文本（保留原方法以备后用）"""
        try:
            tree = ET.parse(xml_file)
            root = tree.getroot()
            
            texts = []
            
            for text_node in root.findall('.//string'):
                text_id = text_node.get('name', '')
                text_content = text_node.text or ''
                mz_value = text_node.get('mz', '')
                
                texts.append({
                    'id': len(texts) + 1,
                    'key': text_id,
                    'mz': mz_value,
                    'text': text_content,
                    'original_lang': 'en'
                })
            
            return texts
        except Exception as e:
            self.log(f"解析XML文件错误: {str(e)}", "error")
            return []

    def translate_text(self, text: str, source_lang: str, target_lang: str) -> str:
        """翻译单个文本 - 直接翻译，无语法保护"""
        if not text.strip():
            return text
        
        mapped_target = self._map_lang_code(target_lang)
    
        if mapped_target == source_lang:
            return text
        
        try:
            # 直接调用Google翻译，不做任何语法保护
            translated = GoogleTranslator(
                source='auto',
                target=mapped_target
            ).translate(text)
            
            return translated
        except Exception as e:
            self.log(f"翻译失败({source_lang}->{mapped_target}): {text[:30]}... ({str(e)})", "warning")
            return text

    def _map_lang_code(self, lang_code: str) -> str:
        """映射语言代码到标准格式"""
        return self.lang_code_mapping.get(lang_code, lang_code)

    def log(self, message: str, level: str = "info"):
        """记录日志到界面"""
        now = datetime.datetime.now().strftime("%H:%M:%S")
        tag = ""
        
        if level == "error":
            tag = "ERROR"
            color = "red"
        elif level == "warning":
            tag = "WARN"
            color = "orange"
        elif level == "success":
            tag = "SUCCESS"
            color = "green"
        else:
            tag = "INFO"
            color = "black"
        
        log_msg = f"[{now}] {tag}: {message}\n"
        
        self.log_text.insert(tk.END, log_msg)
        self.log_text.tag_add(level, "end-1c linestart", "end-1c lineend")
        self.log_text.tag_config(level, foreground=color)
        self.log_text.see(tk.END)
        self.root.update()

    def clear_log(self):
        """清空日志"""
        self.log_text.delete(1.0, tk.END)
        self.log("日志已清空")



if __name__ == "__main__":
    root = tk.Tk()
    app = FGUITranslatorApp(root)
    root.mainloop()