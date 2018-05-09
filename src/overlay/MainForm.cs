/*
 * User: ajaxvs
 * Date: 06.05.2018
 * Time: 17:47
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using poeNavigator.app;
using System.Runtime.InteropServices;

namespace poeNavigator {
	public partial class MainForm : Form {
		//================================================================================
		//WinAPI:
		[DllImport("user32.dll", SetLastError = true)]
		static private extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
		
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static private extern bool UnregisterHotKey(IntPtr hWnd, int id);
		//=====
		public enum GWL {
			ExStyle = -20
		}
		public enum WS_EX {
			Transparent = 0x20,
			Layered = 0x80000
		}
		public enum LWA	{
			ColorKey = 0x1,
			Alpha = 0x2
		}
		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		public static extern int GetWindowLong(IntPtr hWnd, GWL nIndex);
		
		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		public static extern int SetWindowLong(IntPtr hWnd, GWL nIndex, int dwNewLong);
		
		[DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
		public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte alpha, LWA dwFlags);
		//=====
		private const int WM_HOTKEY = 0x0312;
		private const int WM_NCHITTEST = 0x84; 
		private const int HTTRANSPARENT = -1;
		private const int switchHotkeyId = 0;
		//================================================================================
		//dev config:
		//overlay: good looking, no input:
		private const bool isTransparent = true;
		private const bool isPaintBg = false;
		private const bool isLimeBg = false;
		private const bool isLayerStyle = true;    	
		//so.. options:
		//1. "Aero" enabled. Everything is ok.
		//2. "Aero" disabled:
		//2.1. opaque window, handle input, good fps. Bad.
		//2.2. transparent window, ignore input, low fps. Very bad.
		//================================================================================
		//user config:
		private const byte minimizeWindowAlpha = 0x80;
		private const byte fullWindowAlpha = 0xE0;
		private const int noteAddHeight = 5;
		private const double imgAspect = 1.778;
		//================================================================================
		//vars:
		private bool isFullMode = false;
		private PictureBox[] aPictureBoxes;
		private int visiblePictures = 0;
		private int registeredHotkey = 0;
		//================================================================================
		public MainForm(CajConfig config) {
			InitializeComponent();
			
			//default vars:
			this.TopMost = true;
			this.notifyIcon1.Text = CajApp.appName;
			this.Text = CajApp.appName;
			this.lblNotes.Text = "";
			
			//init:
			createSprites();
			createHotkey(config.read(CajConfig.switchHotkey));
			setOverlayTheme();
			setGamePart(config.readInt(CajConfig.gamePart, 1));
		}
		//================================================================================
		private void createSprites() {
			//image containers:	
			const int maxBoxes = 6; //6 max. we have no space for more images.
			aPictureBoxes = new PictureBox[maxBoxes];
			for (int i = 0; i < maxBoxes; i++) {
				PictureBox pic = new PictureBox();				
				pic.SizeMode = PictureBoxSizeMode.StretchImage;
				//null margin (default = 3):
				var margin = pic.Margin;
				margin.Left = 0;
				margin.Right = 0;
				margin.Top = 0;
				margin.Bottom = 0;
				pic.Margin = margin;
				//add to form, don't show empty ones:				
				pic.Visible = false;	
				this.Controls.Add(pic);
				aPictureBoxes[i] = pic;
			}
		}		
		//================================================================================
		/**
		 * @param switchHotkey - Keys value. i.e. "Capital" for CapsLock.
		 * //TODO: change hotkey param to int.
		 * */
		private void createHotkey(string switchHotkey) {
			//RegisterHotKey() notes:
			//prevents key press in current target app.
			//doesn't work if PoE was run by admin and this app wasn't.
			//anyway GetAsyncKeyState() doesn't work here and hooks aren't good idea as well.
			//TODO: UnregisterHotKey on hide or fix GetAsyncKeyState (?). 
			//TODO: notice user to run as admin if needed (?).

			if (switchHotkey == "") return;

			//switch mode hotkey:			
			try {
				registeredHotkey = (int)Enum.Parse(typeof(Keys), switchHotkey);	
				if (registeredHotkey > 0) {
					bool isOk = RegisterHotKey(this.Handle, switchHotkeyId, 0, registeredHotkey);
					CajApp.trace("hotkey set: " + registeredHotkey.ToString() + " " +
				             ((Keys)registeredHotkey).ToString() + ", " + isOk.ToString());
				} else {
					CajApp.trace("no hotkey: " + registeredHotkey.ToString() + " " + switchHotkey);
				}
			} catch (Exception ex) {
				CajApp.trace("exception hotkey (" + switchHotkey + "): " + ex.Message);
				return;
			}	
		}
		//================================================================================
		public void changeHotkey(string newHotkey) {			
			//reset anyway, even if newHotkey == old:
			if (registeredHotkey != 0) {				
				UnregisterHotKey(this.Handle, switchHotkeyId);
			}
			createHotkey(newHotkey);
		}
		//================================================================================
		private void setOverlayTheme() {
			if (isTransparent) {
				Color color;
				if (isLimeBg) {
					color = Color.LimeGreen;
				} else {
					color = Color.Black;
				}
				this.BackColor = color;
				this.TransparencyKey = color;
        		
				if (isLayerStyle) {
					//destroys permormance without "Aero".
					int wl = GetWindowLong(this.Handle, GWL.ExStyle);
					wl = wl | 0x80000 | 0x20; //WS_EX.Layered | WS_EX.Transparent.
					//wl = wl | 0x20; //disappears after dx render.
					SetWindowLong(this.Handle, GWL.ExStyle, wl);
					setWindowAlpha(minimizeWindowAlpha);
				}
			} else {
				this.BackColor = Color.FromArgb(0xFF, 0x21, 0x21, 0x21);
			}
		}
		//================================================================================
		public void setWindowAlpha(byte alpha) {
			SetLayeredWindowAttributes(this.Handle, 0, alpha, LWA.Alpha);
		}
		//================================================================================
		protected override void OnPaintBackground(PaintEventArgs e) {
			//"overlay" no aero bg:
			//redraws once per size changing, it's ok.
			if (isPaintBg) {
				//anyway any transparent GUI == fps killer without "Aero".
				if (isLimeBg) {
					//full transparent without "Aero":
					e.Graphics.FillRectangle(Brushes.LimeGreen, e.ClipRectangle);
				} else {
					//colored without "Aero":
					e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
				}
			} else {
				base.OnPaintBackground(e);
			}
		}
		//================================================================================
		protected override void WndProc(ref Message m) {			
			int msg = m.Msg;
			if (msg == WM_HOTKEY) {
				//int hotkeyId = m.WParam.ToInt32(); //nn. atm we have only one hotkey.
				if (this.Visible) {
					switchMode();
				}
			} else if (msg == WM_NCHITTEST) {
				//another "no input". doesn't work on Win7x64 without SetWindowLong().				
				m.Result = (IntPtr)HTTRANSPARENT;				
			}
			base.WndProc(ref m);
		}
		//================================================================================
		void MainFormLoad(object sender, EventArgs e) {
			onMin();
		}
		//================================================================================
		void MainFormResize(object sender, EventArgs e) {
			if (this.WindowState == FormWindowState.Minimized) {
				CajApp.trace("minimized");
			}
		}
		//================================================================================
		void NotifyIcon1MouseDoubleClick(object sender, MouseEventArgs e) {
			/*
			//show overlay:
			this.WindowState = FormWindowState.Normal;
			onMin();
			this.Show();
			*/
			//options:
			CajApp.showOptions();
		}
		//================================================================================
		void MainFormFormClosed(object sender, FormClosedEventArgs e) {
			notifyIcon1.Visible = false;
		}
		//================================================================================
		void MenuToolStripMenuItemClick(object sender, EventArgs e) {
			CajApp.end();
		}
		//================================================================================
		void ToolStripMenuItem1Click(object sender, EventArgs e) {			
			CajApp.showByNotifyOption(true);
			onFull();
		}
		//================================================================================
		void ToolStripMenuItem2Click(object sender, EventArgs e) {			
			CajApp.showByNotifyOption(true);
			onMin();
		}
		//================================================================================
		void ToolStripMenuItem3Click(object sender, EventArgs e) {			
			CajApp.showByNotifyOption(false);
			onHide();
		}
		//================================================================================
		void NotifyIconAboutButtonClick(object sender, EventArgs e) {
			CajApp.showOptions();
		}
		//================================================================================
		private void onFull() {	
			this.Visible = false;	
			isFullMode = true;
			
			const double partOfScreen = 0.7; //we need some space for notes height.
			double fullImgSize = (Screen.PrimaryScreen.Bounds.Height * partOfScreen) / 3;
			int fullImageWidth = (int)(fullImgSize * imgAspect);
			int fullImageHeight = (int)fullImgSize;
			
			correctImages(fullImageWidth, fullImageHeight);

			//enlarge form:
			lblNotes.Left = (this.Width - lblNotes.Width) / 2;
			lblNotes.Top = this.Height;
			this.Height += lblNotes.Height + noteAddHeight;
			
			this.Left = (Screen.PrimaryScreen.Bounds.Width - this.Width) / 2;
			this.Top = (Screen.PrimaryScreen.Bounds.Height - this.Height) / 2;
			setWindowAlpha(fullWindowAlpha);
			
			lblNotes.Visible = true;
			this.Visible = true;
			correntForm();
			this.Show();			
		}
		//================================================================================
		private void onMin() {
			this.Visible = false;
			isFullMode = false;
			
			//screen.height * 0.25 == poe minimap size.
			double minImgSize = Screen.PrimaryScreen.Bounds.Height * 0.25 / 2;
			int minimizeImageWidth = (int)(minImgSize);
			int minimizeImageHeight = (int)(minImgSize / imgAspect);

			correctImages(minimizeImageWidth, minimizeImageHeight);
			
			this.Left = Screen.PrimaryScreen.Bounds.Width - this.Width - 5;
			this.Top = (int)(Screen.PrimaryScreen.Bounds.Height * 0.25 + 15);
			setWindowAlpha(minimizeWindowAlpha);
			
			lblNotes.Visible = false;			
			this.Visible = true;			
			correntForm();
			this.Show();
			
			//CajApp.trace("min img size: " + minimizeImageWidth.ToString() + " " +
			//	minimizeImageHeight.ToString());
		}
		//================================================================================
		private void correctImages(int imgWidth, int imgHeight) {
			if (visiblePictures == 1) {
				//one image: set double size:
				PictureBox pic = aPictureBoxes[0];
				pic.Left = 0;
				pic.Top = 0;
				pic.Width = imgWidth * 2;
				pic.Height = imgHeight * 2;
				//form:
				this.Width = pic.Width;
				this.Height = pic.Height;
			} else if (visiblePictures == 2) {
				//two images: vertical align:
				int largeWidth = (int)(imgWidth * 1.5);
				int largeHeight = (int)(imgHeight * 1.5);
				//top:
				PictureBox pic = aPictureBoxes[0];
				pic.Left = 0;
				pic.Top = 0;
				pic.Width = largeWidth;
				pic.Height = largeHeight;
				//bottom:
				pic = aPictureBoxes[1];
				pic.Left = 0;
				pic.Top = largeHeight;
				pic.Width = largeWidth;
				pic.Height = largeHeight;	
				//form:
				this.Width = largeWidth;
				this.Height = largeHeight * 2;
			} else {
				//3-6 images: grid:
				int index = 0;
				for (int row = 0; row < 3; row++) {
					for (int col = 0; col < 2; col++) {				
						PictureBox pic = aPictureBoxes[index++];
						pic.Left = col * imgWidth;
						pic.Top = row * imgHeight;
						pic.Width = imgWidth;
						pic.Height = imgHeight;
					}
				}
				//form:
				this.Width = imgWidth * 2;
				if (visiblePictures < 5) {					
					this.Height = imgHeight * 2;
				} else {
					this.Height = imgHeight * 3;
				}	
			}
			
			this.lblNotes.MaximumSize = new Size(this.Width, 0);
			
			//CajApp.trace("correctImages form: " + 
			//this.Width.ToString() + " " + this.Height.ToString());
		}
		//================================================================================
		private void correntForm() {
			if (!isFullMode && visiblePictures == 0) {
				int tinyHeight;
				if (this.lblNotes.Text != "") {
					tinyHeight = 10; //tip: "here's something!"
				} else {
					tinyHeight = 2; //tip: "i'm still working!"
				}
				this.Height = tinyHeight;
			}
		}
		//================================================================================
		private void onHide() {
			this.Hide();
			isFullMode = !isFullMode; //get the same mode after switch hotkey pressed later. 
		}
		//================================================================================
		public void setVisibilitySafe(bool show) {
			Invoke(new Action(() => {
			                  	if (show) {
			                  		this.Show();
			                  	} else {
			                  		this.Hide();
			                  	}
			}));
		}
		//================================================================================
		public void switchMode() {
			if (isFullMode) {
				onMin();
			} else {
				onFull();
			}
		}
		//================================================================================
		public void redraw() {
			isFullMode = !isFullMode;
			switchMode();
		}
		//================================================================================
		public void setLocationData(string[] aImages, string note) {
			Invoke(new Action(() => {
				setImages(aImages);
				setNote(note);
				redraw();
			}));
		}
		//================================================================================
		private void setImages(string[] aImages) {
			visiblePictures = 0;
			for (int i = 0; i < aPictureBoxes.Length; i++) {
				if (aImages != null && i < aImages.Length) {
					aPictureBoxes[i].Image = Image.FromFile(aImages[i]);
					aPictureBoxes[i].Visible = true;
					visiblePictures++;
				} else {
					aPictureBoxes[i].Visible = false;
				}
			}
		}
		//================================================================================
		private void setNote(string txt) {
			lblNotes.Text = txt;
		}
		//================================================================================
		private void setGamePart(int i) {			
			notifyMenuPart1.Checked = (i == 1 ? true : false);
			notifyMenuPart2.Checked = (i == 2 ? true : false);
		}
		//================================================================================
		void NotifyMenuPart1Click(object sender, EventArgs e) {
			setGamePart(1);
			CajApp.setGamePart(1);
		}
		//================================================================================
		void NotifyMenuPart2Click(object sender, EventArgs e) {
			setGamePart(2);
			CajApp.setGamePart(2);
		}
		//================================================================================
	}
}
