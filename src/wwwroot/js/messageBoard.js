

(function () { 

    document.addEventListener('DOMContentLoaded', function () {


        let authUser = appConfig.authUser;
        let sessionMap = new Map([
            ['Public', 'Public']
        ]);
        let currentSession = 'Public';
        let currentUser = 'Public';
        let currentSequenceMessage;
        const generateGuid = () =>
            ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
                (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
            );

       
        let isCollapsed = false;

        
        function htmlToMarkdown(html) {
            let markdown = html;
            markdown = markdown.replace(/<h1>(.*?)<\/h1>/g, '# $1\n');
            markdown = markdown.replace(/<strong>(.*?)<\/strong>/g, '**$1**');
            markdown = markdown.replace(/<em>(.*?)<\/em>/g, '*$1*');
            markdown = markdown.replace(/<p>(.*?)<\/p>/g, '$1\n');
            markdown = markdown.replace(/<img src="(.*?)" alt="(.*?)"[^>]*>/g, '![$2]($1)');
            // Add more replacements as needed
            return markdown;
        }

        function generateThumbnail(file) {
            return new Promise((resolve, reject) => {
                if (!file.type.startsWith('image/')) {
                    reject('Not an image file');
                    return;
                }

                try {
                    const reader = new FileReader();
                    reader.onload = (event) => {
                        const img = new Image();
                        img.onload = () => {
                            const canvas = document.createElement('canvas');
                            const ctx = canvas.getContext('2d');
                            const maxDim = 100;
                            if (img.width > img.height) {
                                canvas.width = maxDim;
                                canvas.height = (img.height / img.width) * maxDim;
                            } else {
                                canvas.height = maxDim;
                                canvas.width = (img.width / img.height) * maxDim;
                            }
                            ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                            resolve(canvas.toDataURL('image/jpeg'));
                        };
                        img.src = event.target.result;
                    };
                    reader.readAsDataURL(file);
                } catch (err) {
                    reject(err);
                }
            });
        }
        function blinktext(id2Element) {
            var f = document.getElementById(`${id2Element}`);
            setInterval(function () {
                f.style.visibility = f.style.visibility == 'hidden' ? '' : 'hidden';
            }, 1000);
        }
        function getByValue(map, searchValue) {
            for (let [key, value] of map.entries()) {
                if (value === searchValue)
                    return key;
            }
        }

        const getRandomColor = () => {
            return `#${Array.from({ length: 6 }, () => Math.floor(Math.random() * 16).toString(16)).join('')}`;
        };

        function getRandomInt(min, max) {
            min = Math.ceil(min); // Ensure min is an integer
            max = Math.floor(max); // Ensure max is an integer
            return Math.floor(Math.random() * (max - min + 1)) + min;
        };

        
        //  Html encode message.
        const encodedMessage = function (message) {
            return message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
        }

        
        const addNewSessionCard = function (roomName, sessionId, lastestMessage, userCount = 0) {
            const entry = document.createElement('li');
            entry.id = sessionId;
            entry.classList.add("p-2", "m-2", "rounded-3", "shadow-sm", "card");

            const now = new Date().getTimezoneOffset().toLocaleString();
            if (sessionId === 'Public') {
                entry.classList.add("bg-secondary")
            } else {
                entry.style.background = getRandomColor();
            }

            entry.innerHTML =
                `<div class="session-card" title="${roomName}" >
                    <div class="session-card-header" style="display: flex; align-items: center; justify-content: space-between;">
                        <div style="display: flex; align-items: center;">
                            <div class="avatar-circle" style="width: 40px; height: 40px; background-color: ${getRandomColor()}; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-weight: bold; color: white;">  
                                ${roomName.charAt(0).toUpperCase()}  
                            </div>
                        </div>
                        <span id="sessiondelete-${sessionId}" type="button" class="delete-icon" style="cursor: pointer;">
                            <i class="fas fa-trash"></i>
                        </span>
                    </div>
                    <div class="session-card-body">
                        <div class="room-name" id="userName-${sessionId}">
                            ${roomName}
                        </div>
                        <div class="latest-message" id="message-${sessionId}">
                            ${lastestMessage}
                        </div>
                    </div>
                </div>`;

            // Fix: Use appendChild to add the badge element
            const div = document.createElement('div'); 
            const it = document.createElement('i'); 
            const badge = document.createElement('span');
            it.className = "fa fa-person float-end"
            it.style.fontSize = "0.9em";
            badge.className = "badge bg-secondary ms-2";
            badge.id = `usercount-${sessionId}`;
            badge.style.fontSize = "0.9em";
            badge.textContent = `  ${userCount}  `;
            div.className = "float-end flex-none"; 
            it.appendChild(badge);  
            div.appendChild(it); 
            entry.appendChild(div);

            entry.addEventListener('click', (event) => {
                function openRoom(roomName) {
                    document.getElementById("collapseMax").classList.add("d-none");
                    document.getElementById("chatView").classList.remove("d-none");
                }
                openRoom(roomName);
                changeSession(roomName, connection);
                console.log(`switch to session to: ${roomName}`);
                const el = document.querySelector(`[data-session-id="${sessionMap.get(roomName)}"]`);
                const name = document.getElementById("rcNameOptions");
                name.value = roomName;
            });

            document.getElementById("userlist").appendChild(entry);

            var session = document.getElementById(`sessiondelete-${sessionId}`);
            session.addEventListener('click', (event) => {
                event.stopPropagation();
                event.preventDefault();
                swal({
                    title: `Would you like to delete Session ?`,
                    text: ` ${roomName}`,
                    icon: "warning",
                    buttons: ["No", "Yes"],
                    dangerMode: true,
                }).then((value) => {
                    if (value) {
                        const el = document.querySelector(`[data-session-id="${sessionMap.get(roomName)}"]`);
                        if (el)
                            el.remove();

                        deleteSession(authUser, roomName, entry)
                        const dropdown = document.getElementById("rcNameOptions");
                        if (dropdown) {
                            // Remove the deleted room from the dropdown
                            const optionToRemove = Array.from(dropdown.options).find(option => option.value === roomName);
                            if (optionToRemove) {
                                dropdown.removeChild(optionToRemove);
                            }

                            // Reset the dropdown to "Public"
                            dropdown.value = "Public";
                        }
                        setTimeout(() => {
                            document.getElementById(sessionMap.get('Public')).scrollIntoView({
                                behavior: "smooth",
                                block: 'center',
                                inline: 'center',
                            });
                        }, 400)
                    } else {
                        console.log("User chose No!");
                    }
                });

            });


        }

        const swapSessionCard = function (currentSession, newSession, targetName) {

            const preSession = document.getElementById(currentSession);
            if (preSession) {
                preSession.style.border = ".3em solid white";
                preSession.style.boxShadow = "2px 4px 8px black";
            }

            const curSession = document.getElementById(newSession);
            if (curSession) {
                curSession.style.border = ".3em solid black";
                curSession.style.boxShadow = "2px 4px 8px white"
            }


            document.getElementById('sessionLabel').innerText = String(targetName).split(`@`, 1).toString();

            const elementId = 'message-' + newSession;
            const sessionCardElement = document.getElementById(elementId);

            if (sessionCardElement) {
                sessionCardElement.innerText = "";
            } 

        }

        function scrollToLastMessage() {
            var messages = document.getElementById('messages');
            if (messages && messages.lastElementChild) {
                messages.lastElementChild.scrollIntoView({ behavior: 'smooth', block: 'end' });
            }
        }


        const updateSessionCard = function (sessionId, lastestMessage) {

            const elementId = 'message-' + sessionId;
            const sessionCardElement = document.getElementById(elementId);

            sessionCardElement.innerHTML = lastestMessage;

        }

        function debounce(func, delay) {
            let timer;
            return function (...args) {
                clearTimeout(timer);
                timer = setTimeout(() => func.apply(this, args), delay);
            };
        }

        const createNewMessage = function (sender, message, messageId, now) {  
           now = now || new Date().toLocaleTimeString('en-US');  
           messageId = messageId || new Date().toLocaleTimeString('en-US');  

           const entry = document.createElement('li');  
           entry.classList.add("py-1", "mx-2", "list-group-item");  
           entry.id = `messageId-${messageId}`;  

            if (sender === "Public") {
                entry.innerHTML = `<div class="text-center text-muted system-message">${message}</div>`;
            } else if (sender === authUser) {
                entry.classList.add("justify-content-start");
                entry.innerHTML = `  
                          <div class="d-flex flex-row align-items-start">  
                              <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-1.webp" alt="avatar"  
                                  class="rounded-circle shadow-1-strong" width="40" style="max-width: 100%; height: auto;" />  
                              <div class="message-bubble border-success text-dark rounded" style="word-wrap: break-word;">
                                  <p class="fw-bold mb-1">${String(sender).split('@@', 1).toString()}</p>  
                                  <p class="mb-0">${message}</p>  
                                  <p class="text-muted small mt-1"><i class="fa fa-clock"></i> ${now}</p>  
                              </div>  
                          </div>`;
            } else {
                entry.classList.add("justify-content-end");
                entry.innerHTML = `  
                          <div class="d-flex flex-row align-items-end">  
                              <div class="message-bubble border-primary text-dark rounded" style="word-wrap: break-word;">
                                  <p class="fw-bold mb-1">${String(sender).split('@@', 1).toString()}</p>  
                                  <p class="mb-0">${message}</p>  
                                  <p class="text-muted small mt-1"><i class="fa fa-clock"></i> ${now}</p>  
                              </div>  
                              <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-4.webp" alt="avatar"  
                                  class="rounded-circle shadow-1-strong" width="40" style="max-width: 100%; height: auto;" />  
                          </div>`;
            }


           return entry;  
        }

        const addNewMessageToScreen = function (messageEntry) {

            messageEntry.classList.add('message-enter');
            const messageBoxElement = document.getElementById('messages');
            messageBoxElement.appendChild(messageEntry);
            messageBoxElement.scrollTop = messageBoxElement.scrollHeight;
            //  Clear text box and reset focus for next comment.
            messageInput.value = '';
            messageInput.focus();

        }

        const createNewMessageStatus = function (messageId, messageStatus) {
            const messageStatusEntry = document.createElement('div');
            messageStatusEntry.classList.add("d-flex", "flex-row", "align-items-end");
            messageStatusEntry.innerHTML =
                `<div class="message-avatar bottom-0 end-0 m-2" id="${messageId}-Status">
                    ${messageStatus}
                </div>`;
            return messageStatusEntry;
        }

        const updateMessageStatus = function (messageId, messageStatus, sequenceId) {
            const statusElement = document.getElementById(messageId + '-Status');
            const messageElement = document.getElementById('messageId-' + messageId);
            statusElement.innerText = messageStatus;
            statusElement.id = sequenceId + '-Status';
            messageElement.id = 'messageId-' + sequenceId;
        }

        const messageInput = document.getElementById('message');
        messageInput.focus();

        const addHistoryMessage = function (historyMessage, connection, sessionId) {
            const messageBoxElement = document.getElementById('messages');

            // Clear messages if the session has changed
            if (currentSession !== sessionId) {
                messageBoxElement.innerHTML = ''; // Clear all messages
                currentSession = sessionId; // Update the current session
            }
            // Track existing messages to avoid duplicates
            const existingMessageIds = new Set([...messageBoxElement.children].map(child => child.id));

            historyMessage.forEach(element => {
                if (!existingMessageIds.has(`messageId-${element.sequenceId}`)) { // Only add new messages
                    const messageEntry = createNewMessage(
                        element.senderName,
                        element.messageContent,
                        element.sequenceId,
                        element.sendTime.substring(11, 19) // Extract time
                    );

                    // Handle message status for private sessions
                    if (currentSession !== 'Public' && element.senderName !== authUser) {
                        if (element.messageStatus !== 'Read') {
                            invoke('sendUserResponse', currentSession, element.sequenceId, element.senderName, 'Read');
                        }
                    } else if (currentSession !== 'Public' && element.senderName === authUser) {
                        const messageStatusEntry = createNewMessageStatus(element.sequenceId, element.messageStatus);
                        messageEntry.appendChild(messageStatusEntry);
                    }

                    addNewMessageToScreen(messageEntry);
                }
            });
        };

        const changeSession = async function (targetName, connection) {
            if (targetName === authUser) {
                alert('You cannot create a session with yourself!');
                return;
            }

            // Check if the session exists locally
            let sessionId;

            if (sessionMap.has(targetName)) {
                sessionId = sessionMap.get(targetName);
            } else {
                sessionId = await invoke('getOrCreateSession', targetName);
                sessionMap.set(targetName, sessionId);
                // allow signalr to create room first in database.
                // Do NOT use recursion here. might cause infinite loop if the session is not created yet.
                // when the server notifies that the session/room has been created and is available.
                return;
            }

            // Swap session cards visually
            swapSessionCard(currentSession, sessionId, targetName);
            // Update the current session user count after changing the session. 
            await invoke('broadcastUserGroupCounts');

            setTimeout(() => {
                scrollToLastMessage();
                document.getElementById('message')?.focus(); 
            }, 1000);

            // Update room name in the dropdown and inputcolumn
            currentUser = targetName;

            // Fetch and display messages for the new session
            const historyMessage = await invoke('loadMessages', sessionId);

            // Add the history messages to the screen
            addHistoryMessage(historyMessage, connection, sessionId);

            return sessionId; // Return the session ID for further use
        };

        const sendUserMessage = async function (connection) {

            messageInput.value = htmlToMarkdown(messageInput.innerHTML);
            if (!messageInput.value) {
                return;
            }

            
            const messageId = generateGuid();
            const sessionId = currentSession;
            const targetName = currentUser;

            //  Create the message in the window && filter is applied.
            const messageText = messageInput.value;
            messageInput.value = '';
            messageInput.innerHTML = '';

            //  Currently we pull all messages from the server
            await changeSession(currentUser, connection);

            //  Create the message in the room.
            const messageEntry = createNewMessage(authUser, messageText, messageId, '');
            const messageStatusEntry = createNewMessageStatus(messageId, 'Sending');
            messageEntry.appendChild(messageStatusEntry);
            addNewMessageToScreen(messageEntry);
            const messageBoxElement = document.getElementById('messages');
            messageBoxElement.scrollTop = messageBoxElement.scrollHeight;

            //  Call the sendUserMessage method on the hub.
            const sequenceId = currentSession === 'Public'
                ? await invoke('broadcastMessage', messageText)
                : await invoke('sendUserMessage', sessionId, targetName, messageText);

            currentSequenceMessage = sequenceId;
            updateMessageStatus(messageId, 'Sent', sequenceId);
            updateSessionCard(sessionId, messageText);
        }

        const deleteSession = function (userName, roomName, element) {
            if (roomName === 'Public') {
                SnackBar({ status: 'error', icon: 'danger', speed: "0.5s", positon: "tc", message: "You can't delete Public room ever!" });
            } else {
                if (roomName !== null) {
                    invoke('deleteUserSession', userName, roomName);
                    element.remove();
                    sessionMap.delete(roomName);
                    SnackBar({ status: 'info', timeout: '2400', icon: 'info', message: ` (${roomName}) room was deleted successfully !` })

                }
            }
        }


        const displayUnreadMessage = function (sessionId, sender, messageContent) {
            var time = new Date().toLocaleTimeString();
            if (sessionMap.has(sender) || sessionId === 'Public') {
                updateSessionCard(sessionId, `<article><p>New Message...     ${time}</p><p class='limited-text'>${messageContent}</p></article>`);
                setTimeout(() => {
                    statusBar.innerHTML = "";
                }, 5000)
                statusBar.innerHTML = "<article class='w-25 p-2'>New Message..." + '\t' + time + "<p>" + htmlToMarkdown(messageContent) + "</p><br>" +
                    "Room name: " + getByValue(sessionMap, sessionId) + "</article>";
            } else {
                sessionMap.set(sender, sessionId);
                addNewSessionCard(sender, sessionId, sender + ': ' + messageContent);
            }

            if (window.Notification && Notification.permission !== 'denied') {
                Notification.requestPermission(function (status) {
                    let n = new Notification('You have a new nessage from' + sender, {
                        body: messageContent
                    });
                });
            }
        }

        const showLoginUsers = function (userNameList, connection) {
            const list = document.getElementById('principalList');

            if (!list)
                return;
            while (list.lastChild) {
                list.lastChild.remove();
            }

            // parent meymyself and I.
            const myself = document.createElement('li');

            myself.innerHTML = `<div><hr>ME:  ${authUser.split("@", 1).toString()}<hr/></div>`;
            list.append(myself);
            myself.classList.add('disabled', "text-dark", "text-truncate");

            fetch("/users/all", {
                method: "GET",
                mode: "cors",
                credentials: "include",
                headers: {
                    "Content-type": "application/json"
                },
                body: null
            }).then((us) => {
                if (!us.ok) {
                    throw new Error("Can't load members; pleade refresh page");
                }
                return us.json();
            }).then((data) => {

                data.forEach(u => {
                    const login = document.createElement('li');
                    login.innerHTML =
                        `<div class="align-items-center">
                                <a href="#" class="d-flex align-items-center p-2">
                                    <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-${getRandomInt(1, 16)}.webp" 
                                         alt="avatar" class="rounded-circle shadow-1-strong" width="32" height="32">
                                    <div class="ms-2 text-truncate" style="max-width: 150px;">${u.split("@", 1).toString()}</div>
                                </a>
                            </div>`

                    if (userNameList.includes(u)) {
                        login.classList.add('active-list-item');
                    }
                    if (authUser !== u) {
                        login.addEventListener('click', function (event) {
                            event.preventDefault();
                            changeSession(u, connection);
                            setTimeout(() => {
                                const sessionElement = document.getElementById(sessionMap.get(u));
                                if (sessionElement) {
                                    // Flash the element (e.g., yellow background for 600ms)
                                    const originalBg = sessionElement.style.backgroundColor;
                                    sessionElement.style.transition = "background-color 0.3s";
                                    sessionElement.style.backgroundColor = "#fff3cd";
                                    setTimeout(() => {
                                        sessionElement.style.backgroundColor = originalBg || "";
                                        // Now scroll into view and open the room
                                        sessionElement.scrollIntoView({
                                            behavior: "smooth",
                                            block: 'center',
                                            inline: 'center'
                                        });

                                        SnackBar({
                                            message: `Opening room: ${u.split("@", 1).toString()}`,
                                            status: "info",
                                            icon: "info",
                                            timeout: 3000,
                                            dismissible: true
                                        })

                                        sessionElement.click();

                                        setTimeout(() => { document.getElementById('message')?.focus(); }, 400);

                                    }, 600);
                                }
                            }, 1000);
                            return false;
                        });
                        list.appendChild(login);
                    }
                });

            }).catch((e) => console.dir(e));
        };

        function onConnected(connection) {
            Notification.requestPermission(function (status) {
                if (status === 'granted') {
                    let n = new Notification('Dear ' + authUser +
                        ': \nYour new messages will be displayed here');
                }
            });

            addNewSessionCard('Public', 'Public', 'Welcome...');
            
            changeSession('Public', connection);

            document.getElementById('userName').innerText = String(authUser).split(`@`, 1).toString();

            document.getElementById('sendmessage').addEventListener('click', (event) => sendUserMessage(connection));

            document.getElementById('message').addEventListener('keydown', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault(); // Prevents adding a new line in the textarea
                    sendUserMessage(connection);
                    return false;
                }
            });

            document.getElementById('receiverName').addEventListener('keypress', async function (event) {

                
                if (event.key == 'Enter') {
                    event.preventDefault();
                    event.stopPropagation();

                    var sessionId = await changeSession(this.value, connection); 

                    const sessionElement = document.getElementById(sessionMap.get(sessionId));
                        if (sessionElement) {
                            sessionElement?.scrollIntoView({
                                behavior: "smooth",
                                block: 'center',
                                inline: 'center'
                            });
                        } else {
                            // default to the last session in the list  
                            const userList = document.getElementById('userlist');
                            if (userList.lastElementChild) {
                                userList.lastElementChild.scrollIntoView({
                                    behavior: "smooth",
                                    block: 'center',
                                    inline: 'center'
                                });
                            }   
                        }
                        
                   
                    this.value = '';
                    return false;
                }
            });

            document.getElementById('rcNameOptions').addEventListener('change', function (event) {

                event.preventDefault();
                setTimeout(() => {  
                   const targetElement = document.getElementById(sessionMap.get(this.value));  
                   if (targetElement) {  
                       targetElement.scrollIntoView({  
                           behavior: "smooth",  
                           block: 'nearest',  
                           inline: 'nearest'  
                       });  
                   }  
                }, 400);
                // default run below
                changeSession(this.value, connection)
                messageInput.value = '';
                messageInput.focus();
                return false;
            });

            document.getElementById('uploadFile').addEventListener('change', function (event) {

                var files = Array.from(event.target.files);
                const container = document.getElementById('thumbnailContainer');
                container.innerHTML = ''; // Clear existing thumbnails

                if (files.length > 0) {


                    const container = document.getElementById('thumbnailContainer');
                    container.innerHTML = ''; // Clear existing thumbnails

                    files.forEach(_file => {

                        var frmData = new FormData();
                        const messageId = generateGuid();
                        const thumbnailElement = document.createElement('img');
                        const msgDiv = document.createElement('div');

                        // add to form data
                        frmData.append(_file.name, _file);

                        // get the file type reject if not a image write url and name

                        generateThumbnail(_file).then(t => {

                            thumbnailElement.src = t;
                            container.appendChild(thumbnailElement);

                            msgDiv.appendChild(thumbnailElement)

                            var result = SendToAzure(frmData);

                            result.then(data => {
                                data.map(item => {
                                    const urlElement = document.createElement('a');
                                    urlElement.href = item.url;
                                    urlElement.innerText = item.fileName;
                                    container.appendChild(urlElement);
                                    msgDiv.appendChild(urlElement);
                                    messageInput.appendChild(msgDiv);
                                });
                            });

                        }).catch(e => {

                            var result = SendToAzure(frmData);

                            result.then(data => {
                                data.map(item => {
                                    const urlElement = document.createElement('a');
                                    urlElement.href = item.url;
                                    urlElement.innerText = item.fileName;
                                    container.appendChild(urlElement);
                                    msgDiv.appendChild(urlElement);
                                    messageInput.appendChild(msgDiv);
                                });
                            });

                        });
                    });
                }
            });

        }

        async function SendToAzure(frmData) {

            try {
                const response = await fetch('MessageBoard/UploadFile', {
                    method: 'POST',
                    body: frmData
                });

                if (response.ok) {
                    const data = await response.json();
                    console.log(data);
                    return data;
                } else {
                    console.error("Error: response not ok");
                    return undefined;
                }
            } catch (error) {
                console.error("Error", error);
                return undefined;
            }

        }

        function OffSetandScroll(elementId) {
            const element = document.getElementById(elementId);
            if (!element) return;

            element.scrollIntoView({
                behavior: "smooth",
                block: "center",
                inline: "center"
            });
        }
   

        function updateSessionOptions(sessions) {
            // Clear existing options
            const rcNameOptions = document.getElementById('rcNameOptions');
            
            while (rcNameOptions.firstChild) {
                rcNameOptions.removeChild(rcNameOptions.firstChild);
            }

            
            const opt = document.createElement('option');
            opt.value = "Public";
            opt.dataset.sessionId = "Public";
            opt.innerHTML = "Public";
            rcNameOptions.appendChild(opt);

            // Add new session options
            sessions.forEach((element, idx) => {
                const opt = document.createElement('option');
                opt.value = element.key;
                opt.dataset.sessionId = element.value.sessionId;
                opt.innerHTML = element.key;
                // Style for dropdown options (lighter than the first)
                opt.style.backgroundColor = "#f8f9fa";
                opt.style.color = "#212529";
                rcNameOptions.appendChild(opt);
            });

            
        }

        /*
            add for singleton if needed.

            const connectionManager = new ConnectionManager("/message");
            const connection = connectionManager.getConnection();
            connection.serverTimeoutInMilliseconds = 120000;
        */

        function clearUserList() {
            const userList = document.getElementById('userlist');
            if (userList) {
                while (!userList.firstChild) {
                    userList.removeChild(userList.firstChild);
                }
            }
            sessionMap.set('Public', 'Public');
        }
        // Define SignalR event handlers

        // 3. Add a function to update the badge when user count changes
        function updateUserCountBadge(sessionId, userCount) {
            const badge = document.getElementById(`usercount-${sessionId}`);
            if (badge) {
                badge.textContent = userCount;
            }
        }

        const signalREventHandlers = {
            sendNotify: (message) => {
                SnackBar({ message: message, status: "success", icon: "plus", timeout: 5000, dismissible: true });
            },
            displayResponseMessage: (sessionId, sequenceId, messageStatus) => {
                if (sessionId !== currentSession) return;
                const statusElement = document.getElementById(sequenceId + '-Status');
                if (statusElement) statusElement.innerText = messageStatus;
            },
            displayUserMessage: (sessionId, sequenceId, sender, messageContent) => {
                if (currentSession !== sessionId) {
                    displayUnreadMessage(sessionId, sender, messageContent);
                    return;
                }
                const messageEntry = createNewMessage(sender, messageContent, sequenceId, '');
                addNewMessageToScreen(messageEntry);
            },
            updateSessions: (sessions) => {
                clearUserList(); 

                sessions.forEach((element) => {
                   
                    if (!sessionMap.has(element.key)) {
                        sessionMap.set(element.key, element.value ? element.value.sessionId : undefined);
                        addNewSessionCard(element.key, element.value ? element.value.sessionId : '', '');
                    }
                                // Add this function near the other session-related functions
                   
                });
                updateSessionOptions(sessions);
            },
            // update badges by sessions
            //updateUserCount: (sessionUserCounts) => {
            //    // sessionUserCounts is expected to be an array of objects: [{ sessionId: '...', userCount: ... }, ...]
            //    if (Array.isArray(sessionUserCounts)) {
            //        sessionUserCounts.forEach(({ sessionId, userCount }) => {
            //            updateUserCountBadge(sessionId, userCount);
            //        });
            //    }
                        updateUserCount: (sessionUserCounts) => {
                            // sessionUserCounts is expected to be an object: { sessionId1: count1, sessionId2: count2, ... }
                            if (sessionUserCounts && typeof sessionUserCounts === 'object') {
                                Object.entries(sessionUserCounts).forEach(([sessionId, userCount]) => {
                                    updateUserCountBadge(sessionId, userCount);
                                });
                            }
                        },
            //},
            showLoginUsers: (userNameList) => {
                showLoginUsers(userNameList, connection);
            },
            deletedRoom: (session) => {
                SnackBar({ message: "A room has been removed by you from another session", status: "success", icon: 'minus', timeout: 4000, dismissible: true });
            },
            sendPrivateMessage: (sessionId, message, emailText, sender) => {
                if (currentSession === sessionId) return;
                const snack = SnackBar({
                    message: 'View Message',
                    dismissable: true,
                    timeout: 40000,
                    icon: 'plus',
                    actions: [
                        {
                            text: message,
                            dismiss: true,
                            function: function () {
                                changeSession(sender, connection);

                                setTimeout(() => {
                                    var sessionCard = document.getElementById(sessionId).scrollIntoView({
                                        behavior: "smooth",
                                        block: 'center',
                                        inline: 'center'
                                    }).click();


                                    setTimeout(() => { document.getElementById('message')?.focus(); }, 400);

                                }, 1000);
                            }
                        }
                    ]
                });
            }
        };

        // Handle connection errors
        const onConnectionError = (error) => {
            if (error) {
                SnackBar({
                    message: "Connection failure....",
                    status: "error",
                    icon: "danger",
                    timeout: 5000,
                    dismissible: true
                });
            }
        };
        // Start the connection
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/message")
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.serverTimeoutInMilliseconds = 240000;

        bindEvents(signalREventHandlers);
        
        connection.start()
            .then(function () {
                onConnected(connection);
                console.log("AzureChat has STARTED " + new Date().toLocaleString());
            })
            .catch(function (error) {
                console.dir(error);
                onConnectionError(error);
            });

        connection.onclose(function () {
            setTimeout(function () {
                connection.start().then(t => onConnected(connection)).catch(err => onConnectionError(err));
            }, 5000);
            console.log("chat has been disconnected..  " + new Date().toLocaleString());
        });

        function bindEvents(eventHandlers) {
            connection.eventHandlers = eventHandlers; // Save for re-binding after reconnect
            for (const [eventName, handler] of Object.entries(eventHandlers)) {
                connection?.off(eventName); // Remove previous handler if any
                connection?.on(eventName, handler);
            }
        }

        async function invoke(methodName, ...args) {
            try {
                return await connection.invoke(methodName, ...args);
            } catch (error) {
                console.error(`Error invoking SignalR method '${methodName}':`, error);
                throw error;
            }
        }

    });
    
})();