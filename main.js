/*
Copyright (c) 2020, pedro-arruda-moreira
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

// Constants
var REALTIME = 256;
var HIGH = 128;
var ABOVE_NORMAL = 32768;
var NORMAL = 32;
var BELOW_NORMAL = 16384;
var IDLE = 64;

var ERROR_MSG = "Sorry, this JS file will only work on WScript (MS Windows) =(";

// Change this if you nedd to install in other path. (must end with backslash)
var INSTALL_PATH = "C:\\users\\public\\auto_priority_changer\\";
/**
 * Loads a file and returns its content as a string
 */
function include(fso, jsFile) {
    var f, s;
    f = fso.OpenTextFile(jsFile);
    s = f.ReadAll();
    f.Close();
    return s; 
}

/**
 * This function reads the configuration and changes the priority
 * of processes accordingly.
 */
function loop(wmi, fso) {
	var config = null;
	var query = "Select Name FROM Win32_process WHERE ";
	eval('config=' + include(fso, INSTALL_PATH + "config.js") + ';');
	var processList = config.list;
	var priority = config.priority;
	var i = 0;
	for(; i < processList.length; i++) {
		if(i > 0) {
			query = query + " OR ";
		}
		query = query + " Name = '" + processList[i] + "' ";
	}
	var processes = wmi.ExecQuery(query);

	var e = new Enumerator (processes);
	for (; !e.atEnd(); e.moveNext()) {
		var p = e.item();
		try {
			var ret = p.SetPriority(priority);
			if(ret != 0) {
				//WScript.Echo('return ' + ret + ' for process ' + p.Name);
			}
		} catch(e) {
			// TODO: catch
		}
	}
	return config.timeout;
}

/**
 * script entry point
 */
function main() {
	if(WScript) {
		if(WScript.Arguments.length == 0) {
			WScript.Sleep(5000);
			var shell = WScript.CreateObject("Shell.Application");
			shell.ShellExecute("C:\\windows\\system32\\wscript.exe",
				INSTALL_PATH + 'main.js 1', "", "runas");
			return;
		}
		// fso and wmi are cached for performance
		var fso = new ActiveXObject("Scripting.FileSystemObject"); 
		var wmi = GetObject("winmgmts:");
		while(true) {
			var timeout = loop(wmi, fso);
			WScript.Sleep(timeout * 1000);
		}
	} else if(alert) {
		alert(ERROR_MSG);
	} else if(console && console.log) {
		console.log(ERROR_MSG);
	}
}

main();