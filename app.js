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

// START: ===================== Constants =====================

// Priorities
var REALTIME = 256;
var HIGH = 128;
var ABOVE_NORMAL = 32768;
var NORMAL = 32;
var BELOW_NORMAL = 16384;
var IDLE = 64;

// Log
var LOG_ERROR = 1;
// END:   ===================== Constants =====================

/**
 * This function reads the configuration and changes the priority
 * of processes accordingly.
 */
function loop(wmi, shell) {
	
	/**
	 * logs an exception if it happens and log is enabled
	 */
	function log(e) {
		if(!hasArgument('no-log')) {
			var msg = 'Auto Priority Changer error log:\n';
			if(e.toString) msg += e.toString() + '\n';
			if(e.message) msg += 'message: ' + e.message + '\n';
			if(e.description) msg += 'desc: ' + e.description + '\n';
			if(e.name) msg += 'name: ' + e.name + '\n';
			if(e.number) msg += 'number: ' + e.number + '\n';
			if(e.lineNumber) msg += 'line num: ' + e.lineNumber + '\n';
			if(e.stack) msg += 'stack: ' + e.stack + '\n';
			shell.LogEvent(LOG_ERROR, msg);
		}
	}
	
	function getConfigFile() {
		if(ARGUMENTS['config-file'] == null) {
			return 'config.js';
		}
		return ARGUMENTS['config-file'];
	}
	
	var config = null;
	var query = "Select Name FROM Win32_process WHERE ";
	try {
		eval('config=' + include(INSTALL_PATH + getConfigFile()) + ';');
	} catch(e) {
		log(e);
		WScript.Echo('Configuration file is corrupted.\n'
			+ 'Will try to reload in 5 minutes.\n\n'
			+ 'Please check syntax.');
		return 300;
	}
	try {
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
				if(ret != 0 && hasArgument('do-echo')) {
					WScript.Echo('Error ' + ret + ' when changing priority for process ' + p.Name);
				}
			} catch(e) {
				log(e);
			}
		}
	} catch(e) {
		log(e);
	}
	return config.timeout;
}

/**
 * script entry point
 */
(function() {	
	if(!hasArgument('no-elevate')) {
		WScript.Sleep(5000);
		var shell = WScript.CreateObject("Shell.Application");
		shell.ShellExecute("wscript.exe",
			'"' + INSTALL_PATH + WScript.ScriptName
				+ '" --no-elevate "'
				+ ARGUMENTS.join('" "')
				+ '"',
			"", "runas");
		return;
	}
	// shell and wmi are cached for performance
	var wmi = GetObject("winmgmts:");
	var shell = WScript.CreateObject("Wscript.Shell");
	while(true) {
		var timeout = loop(wmi, shell);
		WScript.Sleep(timeout * 1000);
	}
}());