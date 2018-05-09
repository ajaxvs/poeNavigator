/*
 * User: ajaxvs
 * Date: 07.05.2018
 * Time: 17:51
 */
using System;
using System.IO;
using System.Text;

namespace poeNavigator.app
{
	public class CajClient {
		//================================================================================
		private const string logsRelativePath = "logs/Client.txt";
		private const string textEntered0 = "] : You have entered ";
		private const string textEntered1 = ".";		
		//================================================================================
		private string clientTxtPath;
		private Action<string> onLocationChange;
		private Action<string> log;		
		private long lastReadOffset = 0;
		//================================================================================
		public CajClient(string clientFolder, Action<string> onLocationChange, Action<string> log) {
			setFolder(clientFolder);
			this.onLocationChange = onLocationChange;			
			this.log = log;
		}
		//================================================================================
		private void findNewLocation(ref string s) {			
			int i0 = s.LastIndexOf(textEntered0, StringComparison.InvariantCulture);
			if (i0 > 0) {
				i0 += textEntered0.Length;
				int i1 = s.IndexOf(textEntered1, i0, StringComparison.InvariantCulture);
				if (i1 > 0) {
					string loc = s.Substring(i0, i1 - i0);
					if (onLocationChange != null) {
						onLocationChange(loc);
					}
				}
			}
		}
		//================================================================================
		public void setFolder(string clientFolder) {
			if (!clientFolder.EndsWith("\\", StringComparison.InvariantCulture) &&
			    !clientFolder.EndsWith("/", StringComparison.InvariantCulture)) {
				clientFolder += "/";
			}
			
			clientTxtPath = clientFolder + logsRelativePath;
			//don't care if it doesn't exist yet.
		}
		//================================================================================
		public void update() {
			string s = "";
			
			try {
				if (clientTxtPath == "" || !System.IO.File.Exists(clientTxtPath)) {
					return; //np. user could delete the old client.txt
				}
				
				FileStream fs = new FileStream(clientTxtPath, 
				           FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				
				//create buffer:
				long fsLength = fs.Length;
				int bytesLeft = (int)(fsLength - lastReadOffset);
				if (bytesLeft <= 0) return;

				//read last symbols:
				fs.Seek(lastReadOffset, SeekOrigin.Begin);
				byte[] bytes = new byte[bytesLeft];
				int numBytesRead = 0;            	
				while (bytesLeft > 0) {
					int n = fs.Read(bytes, numBytesRead, bytesLeft);
					if (n == 0) {
						break;
					}
					numBytesRead += n;
					bytesLeft -= n;
				}
				lastReadOffset = fsLength;
				//fs.Close(); //nn.
				
				//get text:
				s = Encoding.UTF8.GetString(bytes);
            	
			} catch (Exception ex) {
				if (log != null) {
					log("can't read client: " + ex.Message);
				}
			}
			
			//check text:
			if (s != "") {
				findNewLocation(ref s);
			}
		}
		//================================================================================
	}
}
