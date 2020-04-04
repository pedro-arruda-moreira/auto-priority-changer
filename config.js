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

// Auto Priority Changer configuration.

{
	/**
	 * Possible values are:
	 * REALTIME
	 * HIGH
	 * ABOVE_NORMAL -> recommended and default
	 * NORMAL
	 * BELOW_NORMAL
	 * IDLE
	 */
	priority: ABOVE_NORMAL,
	/**
	 * Script will run at every [timeout] seconds.
	 * During execution, this config file will 
	 * also be reloaded.
	 * default: 60
	 */
	timeout: 60,
	/**
	 * list of processes to change execution priority
	 */
	list: [
		"WINWORD.EXE",
		"Excel.exe",
		"Steam.exe",
		"explorer.exe",
		"OneDrive.exe",
		"chrome.exe",
		"iexplore.exe",
		"evelauncher.exe",
		"csgo.exe",
		"eclipse.exe",
		"java.exe",
		"javaw.exe",
		"exefile.exe"
	]
}