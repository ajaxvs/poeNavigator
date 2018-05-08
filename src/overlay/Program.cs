/*
 * User: ajaxvs
 * Date: 06.05.2018
 * Time: 17:47
 */
using System;
using System.Windows.Forms;
using poeNavigator.app;

namespace poeNavigator {
	internal sealed class Program {
		[STAThread]
		private static void Main(string[] args) {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm frm = CajApp.start();			
			Application.Run(frm);			
		}		
	}
}
