/*
 * User: ajaxvs
 * Date: 03.04.2018
 * 
 * Simple config.
 * Auto flush after each save.
 * 
 * //TODO: remove consts, save class as base.
 * 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

namespace poeNavigator.app
{
	public class CajConfig {
		//================================================================================
		public const string poeFolderPath = "poeFolderPath";
		public const string switchHotkey = "switchHotkey";
		public const string gamePart = "gamePart";
        	//================================================================================
		private String path;
		private Action<String> funLog = null;
		private Dictionary<String, String> aConfig;
		//================================================================================
		public CajConfig(String path, Action<String> funLog) {
			this.path = path;
			this.funLog = funLog;
			
			loadFile();
		}
		//================================================================================
		private void loadFile() {
			try {
				aConfig = new Dictionary<String, String>();
				
				string text = System.IO.File.ReadAllText(path);
				string[] aList = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
				for (int i = 0; i < aList.Length; i++) {
					string s = aList[i];
					if (s.StartsWith("//", StringComparison.InvariantCulture)) {
						s = ""; //comment line, ignore
					}
					if (s != "") {
						int pos = s.IndexOf("=", StringComparison.InvariantCulture);
						if (pos > 0) {
							string key = s.Substring(0, pos);
							string value = s.Substring(pos + 1);
							aConfig[key] = value;
						}
					}
				}
			} catch (Exception ex) {
				if (funLog != null) funLog("can't save config: " + ex.Message);
			}
		}
		//================================================================================
		public void write(String key, String value, bool needFlush = true) {
			aConfig[key] = value;
			if (needFlush) {
				flush();
			}
		}
		//================================================================================
		public String read(String key) {
			try {
				return aConfig[key];
			} catch (Exception ex) {
				//it's ok, just not found a key.
				Debug.WriteLine("config load: not found: " + ex.Message);
				return "";
			}
		}
		//================================================================================
		public int readInt(String key, int defaultValue) {
			String value = read(key);
			if (value != "") {
				return int.Parse(value);
			} else {
				return defaultValue;
			}
		}
		//================================================================================
		public void flush() {
			try {
				//format:
				string s = "";
				foreach (KeyValuePair<String, String> line in aConfig) {
					s += line.Key + "=" + line.Value + "\n";
				}
				//save to temp file:
				string tmpPath = path + ".tmp";				
				System.IO.File.WriteAllText(tmpPath, s);
				//if it's ok then rename:
				System.IO.File.Delete(path);
				System.IO.File.Move(tmpPath, path);			
			} catch (Exception ex) {
				if (funLog != null) funLog("can't save config file: " + ex.Message);
			}
		}
		//================================================================================		
	}
}
