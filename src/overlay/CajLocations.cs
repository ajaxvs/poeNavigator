/*
 * User: ajaxvs
 * Date: 07.05.2018
 * Time: 18:42
 */
using System;

namespace poeNavigator.app
{
	public class CajLocations {
		//================================================================================
		private const string noteFileName = "note.txt";
		private const string imgPattern = "*.png";
		//================================================================================
		private string locationsDir;
		private Action<string[], string> setLocationData;
		private Action<string> log;
		private int gamePart = 1;
		private string lastLocation = "";
		//================================================================================
		public CajLocations(string locationsDir,
		                    Action<string[], string> setLocationData,
		                    Action<string> log) 
		{
			this.locationsDir = locationsDir;
			this.setLocationData = setLocationData;
			this.log = log;
		}
		//================================================================================
		public void setPart(int gamePart) {
			//PoE has the same location name for different locations in part1 and part2.
			//it's possible to check what's current part playing with some dirty methods only afaik.
			//i.e. getting location level with pixel search (requires to press "tab").
			//so let's keep it as an user option:
			this.gamePart = gamePart;
			
			if (lastLocation != "") {
				//refresh data:
				onLocationChange(lastLocation);
			}
		}
		//================================================================================
		private string getFolder(string loc) {
			string s = loc.Replace(" ", "_");
			s = s.Replace("'", "_");
			s = locationsDir + s;
			
			if (gamePart > 1) {
				try {
					string suffixLoc = s + "_part" + gamePart.ToString();
					if (System.IO.Directory.Exists(suffixLoc)) {
						s = suffixLoc;
					}
				} catch (Exception ex) {
					log("can't getFolder() " + loc + " " + ex.Message);
				}
			}
			
			return s + "/";
		}
		//================================================================================
		public string[] findImages(string loc) {			
			string[] aImages;
			try {
				string imgFolder = getFolder(loc);
				if (System.IO.Directory.Exists(imgFolder)) {
					aImages = System.IO.Directory.GetFiles(imgFolder, imgPattern);
					log(loc + " " + aImages.Length + " images");
				} else {
					//usually it's location without maps. i.e. city. so it's ok.
					aImages = null;
				}
			} catch (Exception ex) {
				log("can't set images: " + loc + " " + ex.Message);
				aImages = null;
			}

			return aImages;
		}
		//================================================================================
		private string findNote(string loc) {
			string locNotePath = getFolder(loc) + noteFileName;
			string txt = "";
			try {
				if (System.IO.File.Exists(locNotePath)) {
					txt = System.IO.File.ReadAllText(locNotePath);
				}
			} catch (Exception ex) {
				log("can't read note: " + loc + " " + ex.Message);				
			}
			
			return txt;
		}
		//================================================================================
		public void onLocationChange(string loc) {
			lastLocation = loc;
			if (setLocationData != null) {
				setLocationData(findImages(loc), findNote(loc));
			}
		}		
		//================================================================================
	}
}
