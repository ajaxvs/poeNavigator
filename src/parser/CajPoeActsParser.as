package
{
	import flash.filesystem.File;
	import flash.filesystem.FileMode;
	import flash.filesystem.FileStream;
	
	import mx.utils.StringUtil;

	/**
	 * poe acts doc file parsing. 
	 * for inner usage only.
	 */
	public class CajPoeActsParser {
		//================================================================================
		//private const example:RegExp = new RegExp("(?<=START).*?(?=END)", "gs");
		
		private const regCol:RegExp = new RegExp("<td.*?>(.*?)<\/td>", "gs");
		private const regImg:RegExp = new RegExp("(?<=src=\").*?(?=\")", "gs");
		private const regNotes:RegExp = new RegExp("(?<=<span).*?(?=</span>)", "gs");
		private const regAttributes:RegExp = new RegExp(".*?>", "");
		
		private const regTables:RegExp = new RegExp("<table(.*?)<\/table>", "gs");
		private const regRows:RegExp = new RegExp("<tr(.*?)<\/tr>", "gs");
		private const regSpans:RegExp = new RegExp("<span.*?>(.*?)<\/span>", "gs");
		private const regSpecSymbols:RegExp = new RegExp("[\t\n\r]", "g");

		private const regTags:RegExp = new RegExp("<.*?>", "g");
		private const regRoundBrackets:RegExp = /\(.*?\)/g
		private const regQuotes:RegExp = new RegExp("&.squo;", "g");
		
		private const regLocationFolderName:RegExp = new RegExp("[ ']", "g");
		//================================================================================
		private const locationsFileName:String = "locations.txt";
		private const logFileName:String = "log.txt";
		//================================================================================
		private var htmlFileName:String = "";
		private var outDir:String = "";
		private var onSuccess:Function = null;
		
		private var htmlDir:String = "";
		private var aStructIssues:Vector.<String> = new Vector.<String>;
		private var aLocations:Vector.<CajLocation> = new Vector.<CajLocation>;
		private var log:String = "";
		//================================================================================
		/**
		 * Can throw new Error(sErrMsg) if something is wrong.
		 * 
		 * @param htmlFileName
		 * @param outDir
		 * @param onSuccess - optional function():void
		 */
		public function CajPoeActsParser(htmlFileName:String, outDir:String, onSuccess:Function = null) {
			var html:String;
			
			this.htmlFileName = htmlFileName;
			this.outDir = outDir;
			this.onSuccess = onSuccess;
			
			validate();
			html = readHtml();
			parse(html);
			
			checkRepeats();
			checkStructIssues();
			saveResults();
			saveLog();
			
			if (onSuccess != null) {
				onSuccess();
			}
		}
		//================================================================================
		private function validate():void {
			//check out directory:  
			var f:File = new File(outDir);
			if (f.exists) {
				if (!f.isDirectory) {
					throw new Error("Can't create out directory. File already exists: " + outDir);
				}
			} else {
				f.createDirectory();
			}
			
			//check path ending symbol:
			var s:String = outDir.substr(-1);
			if (s != "\\" && s != "/") {
				outDir += "/";
			}

			//save & delete tmp file for test:			
			var tmpFile:String = outDir + "test.tmp";
			saveTextFile(tmpFile, "test");
			f = new File(tmpFile);
			if (f.exists) {
				f.deleteFile();
			} else {
				throw new Error("Can't save results in out directory:" + tmpFile);
			}
		}
		//================================================================================
		private function readHtml():String {
			var html:String;
			var f:File;
			
			//check if file exists:
			f = new File(htmlFileName);
			if (!f.exists) {
				throw new Error("Can't find file: " + htmlFileName);
			}

			//read all:
			var fs:FileStream = new FileStream();
			fs.open(f, FileMode.READ);
			html = fs.readUTFBytes(f.size);
			fs.close();
			if (html.length == 0) {
				throw new Error("The file is empty: " + htmlFileName);
			}
			
			//get main dir:
			htmlDir = f.parent.nativePath + "/";
			
			return html;
		}
		//================================================================================
		private function parse(html:String):void {	
			var i:int;

			//cut prefix:
			var entry:String = "Act One";
			i = html.indexOf(entry);
			if (i == -1) {
				throw new Error("Wrong file. Can't find entry point");
			}
			html = html.substr(i + entry.length);
			
			//get tables:			
			var aTables:Array = html.match(regTables);			
			for each (var tableHtml:String in aTables) {
				//get rows:
				var aRows:Array = tableHtml.match(regRows);
				if (aRows.length > 2) {
					//create row locations and get names, row 0:
					var aNames:Array = aRows[0].match(regSpans);
					var rowLocation:CajLocation;
					var aRowLocations:Vector.<CajLocation> = new Vector.<CajLocation>;
					for each (var name:String in aNames) {
						rowLocation = new CajLocation();
						name = parseName(name);
						name = name.replace(regSpecSymbols, "");
						rowLocation.name = StringUtil.trim(name);
						aRowLocations.push(rowLocation);
					}
					//get images, row 1:
					if (aRows.length > 0) {
						parseColumn(aRows[1], aRowLocations, parseImages);
					}
					//get notes:
					if (aRows.length > 1) {
						parseColumn(aRows[2], aRowLocations, parseNotes);
					}
					//after parsing:
					for each (rowLocation in aRowLocations) {
						saveLocation(rowLocation);
					}
				}
			}
		}
		//================================================================================
		private function parseName(tag:String):String {
			tag = tag.replace(regTags, "");
			tag = tag.replace(regRoundBrackets, "");
			tag = tag.replace(regQuotes, "'");			
			return tag;
		}
		//================================================================================
		/**
		 * @param html
		 * @param aRowLocations
		 * @param parseCell - function(column:String, location:CajLocation):Boolean
		 */
		private function parseColumn(html:String, aRowLocations:Vector.<CajLocation>, parseCell:Function):void {
			if (aRowLocations == null || aRowLocations.length == 0) return;
			
			var aCols:Array = html.match(regCol);
			var loc:int = 0;
			var lastName:String = "";
			for each (var col:String in aCols) {
				if (loc >= aRowLocations.length) {
					//TODO: fix table_struct_issue.
					var issue:String = lastName;
					if (aStructIssues.indexOf(issue) == -1) {
						aStructIssues.push(issue);
					}
					return; 
				}
				lastName = aRowLocations[loc].name;
				parseCell(col, aRowLocations[loc]);
				loc++;
			}
		}
		//================================================================================
		/**
		 * @param column
		 * @param location
		 * @return - true if images were found. 
		 */
		private function parseImages(column:String, location:CajLocation):Boolean {
			var isFound:Boolean = false;
			var aImgs:Array = column.match(regImg);
			if (aImgs && aImgs.length > 0) {
				for each (var img:String in aImgs) {
					img = img.replace("images/", ""); //optional.
					location.aImages.push(img);					
				}
				isFound = true;
			}				
			return isFound;
		}
		//================================================================================		
		private function parseNotes(column:String, location:CajLocation):Boolean {
			var isFound:Boolean = false;
			var aNotes:Array = column.match(regNotes);
			if (aNotes && aNotes.length > 0) {
				var note:String = aNotes[0];
				note = note.replace(regAttributes, ""); 
				note = note.replace(regTags, "");
				note = note.replace(regSpecSymbols, "");
				note = note.replace(regQuotes, "'");
				location.note = note;
				isFound = true;
			}
			return isFound;
		}
		//================================================================================
		private function saveLocation(location:CajLocation):void {
			/*
			//debug:
			trace(location.name);
			for each (var img:String in location.aImages) {
				trace(img);
			}
			trace(location.note);
			trace("===");
			*/
			
			aLocations.push(location);
		}
		//================================================================================
		private function addLog(msg:String):void {
			trace(msg);
			log += msg + "\r\n";
		}
		//================================================================================
		private function checkRepeats():void {
			//debug:
			var repeats:int = 0;
			for (var i:int = 0; i < aLocations.length; i++) {
				for (var j:int = 0; j < aLocations.length; j++) {
					if (i != j && aLocations[i].name == aLocations[j].name) {
						addLog("repeat #" + (++repeats) + ": " + aLocations[i].name);
					}
				}
			}
		}
		//================================================================================
		private function checkStructIssues():void {
			//debug:
			for (var i:int = 0; i < aStructIssues.length; i++) {
				addLog("issue #" + i.toString() + ": " + aStructIssues[i]);
			}
		}
		//================================================================================
		private function saveResults():void {
			//debug:
			addLog("correct tables = " + aLocations.length);
			
			//save json:
			var json:String = JSON.stringify(aLocations, null, 4);
			saveTextFile(outDir + locationsFileName, json);
			addLog("saved json");
			
			//copy files to out dir:
			var f:File;
			for each (var location:CajLocation in aLocations) {
				var path:String = outDir + convertLocationToFolderName(location.name);
				var originalPath:String = path;
				//add suffix for repeats:
				var suffix:int = 2;
				for (;;) {
					f = new File(originalPath);
					if (!f.exists) {
						path = originalPath;
						break;
					}
					originalPath = path + "_part" + suffix.toString();
					suffix++;
				}
				path += "/";
				//save images:
				for each (var img:String in location.aImages) {
					f = new File(htmlDir + "images/" + img);
					if (f.exists) {
						f.copyTo(new File(path + img), true);
					} else {
						addLog("Can't find image: " + img);
					}
				}
				//save note:
				saveTextFile(path + "note.txt", location.note);
			}
			addLog("saved all files");
		}
		//================================================================================
		private function convertLocationToFolderName(name:String):String {
			return name.replace(regLocationFolderName, "_");
		}
		//================================================================================
		private function saveLog():void {
			//flush log:
			saveTextFile(outDir + logFileName, log);
		}
		//================================================================================
		private function saveTextFile(filePath:String, text:String):void {
			var fs:FileStream = new FileStream();
			var f:File = new File(filePath);
			fs.open(f, FileMode.WRITE);
			fs.writeUTFBytes(text);
			fs.close();
		}
		//================================================================================
	}
}

//================================================================================

class CajLocation {
	public var name:String = "";
	public var aImages:Vector.<String> = new Vector.<String>;
	public var note:String = ""; 
	public function CajLocation() {}
}

//================================================================================