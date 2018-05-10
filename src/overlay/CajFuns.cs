/*
 * User: ajaxvs
 * Date: 10.05.2018
 * Time: 17:23
 */
using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace poeNavigator.app
{
	public class CajFuns {
		//================================================================================
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
		//================================================================================
		public CajFuns() {}
		//================================================================================
		static public string getAppPath() {
			return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\";
		}
		//================================================================================
		static public string getForegroundWindowTitle() {
			const int size = 1024;
			StringBuilder sb = new StringBuilder(size);
			IntPtr h = GetForegroundWindow();			
			string ret = "";
			if (GetWindowText(h, sb, size) > 0) {
				ret = sb.ToString();
			}
			return ret;
		}
		//================================================================================
		static public string findPoeFolder() {
			string poeFolder = "";
			try {
				const string steam = "C:/Program Files (x86)/Steam/steamapps/common/Path of Exile/";
				if (System.IO.Directory.Exists(steam)) {
					poeFolder = steam;
				}
				if (poeFolder == "") {
					const string progs = "C:/Program Files (x86)/Grinding Gear Games/Path of Exile/";
					if (System.IO.Directory.Exists(progs)) {
						poeFolder = progs;
					}
				}
				//don't touch win registry.
			} catch (Exception ex) {
				Debug.WriteLine("autoDetectPoeFolder() ex: " + ex.Message);
			}
			return poeFolder;
		}		
		//================================================================================
	}
}
