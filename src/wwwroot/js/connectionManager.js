//// connectionManager.js
export class ConnectionManager {
    constructor(hubUrl) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.serverTimeoutInMilliseconds = 120000;
        this.eventHandlers = {};
    }

    async startConnection(onConnectedCallback, onErrorCallback) {
        try {
            await this.connection.start();
            console.log("SignalR connection started successfully.");
            if (onConnectedCallback) onConnectedCallback(this.connection);
        } catch (error) {
            console.error("Error starting SignalR connection:", error);
            if (onErrorCallback) onErrorCallback(error);
        }
    }

    // Listen for reconnect events and re-bind handlers
    setupReconnectHandlers(onConnectedCallback, onErrorCallback) {
        this.connection.onreconnecting((error) => {
            console.warn("SignalR reconnecting...", error);
        });

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected. ConnectionId:", connectionId);
            // Re-bind all event handlers after reconnect
            this.bindEvents(this.eventHandlers);
            if (onConnectedCallback) onConnectedCallback(this.connection);
        });

        this.connection.onclose(async (error) => {
            console.warn("SignalR connection closed. Attempting to restart...");
            if (onErrorCallback) onErrorCallback(error);
            // Optionally, try to restart connection after a delay
            setTimeout(() => this.startConnection(onConnectedCallback, onErrorCallback), 5000);
        });
    }

    bindEvents(eventHandlers) {
        this.eventHandlers = eventHandlers; // Save for re-binding after reconnect
        for (const [eventName, handler] of Object.entries(eventHandlers)) {
            this.connection.off(eventName); // Remove previous handler if any
            this.connection.on(eventName, handler);
        }
    }

    getConnection() {
        return this.connection;
    }

    async invoke(methodName, ...args) {
        try {
            return await this.connection.invoke(methodName, ...args);
        } catch (error) {
            console.error(`Error invoking SignalR method '${methodName}':`, error);
            throw error;
        }
    }
}

//export class ConnectionManager {
//    constructor(hubUrl) {
//        this.connection = new signalR.HubConnectionBuilder()
//            .withUrl(hubUrl)
//            .withAutomaticReconnect()
//            .configureLogging(signalR.LogLevel.Information)
//            .build();

//        this.connection.serverTimeoutInMilliseconds = 120000; // Set server timeout
//    }

//    // Start the connection
//    async startConnection(onConnectedCallback, onErrorCallback) {
//        try {
//            await this.connection.start();
//            console.log("SignalR connection started successfully.");
//            if (onConnectedCallback) onConnectedCallback(this.connection);
//        } catch (error) {
//            console.error("Error starting SignalR connection:", error);

//            if (error) {
//                if (onErrorCallback) onErrorCallback(error);
//            }
            
//        }
//    }

//    // Reconnect on close
//    handleReconnection(onConnectedCallback, onErrorCallback) {
//        this.connection.onclose(async (error) => {
//            console.warn("SignalR connection closed. Attempting to reconnect...");
//            if (error) {
//                console.error("Connection closed with error:", error);
//            }

//            let retryCount = 0;
//            const maxRetries = 5;

//            while (retryCount < maxRetries) {
//                try {
//                    await this.startConnection(onConnectedCallback, onErrorCallback);
//                    console.log("Reconnection successful.");
//                    return;
//                } catch (reconnectError) {
//                    retryCount++;
//                    console.error(`Reconnection attempt ${retryCount} failed:`, reconnectError);
//                    const delay = Math.min(5000 * retryCount, 30000); // Exponential backoff
//                    await new Promise((resolve) => setTimeout(resolve, delay));
//                }
//            }

//            console.error("Max reconnection attempts reached. Giving up.");
//        });
//    }

//    // Bind SignalR events
//    bindEvents(eventHandlers) {
//        for (const [eventName, handler] of Object.entries(eventHandlers)) {
//            this.connection.on(eventName, handler);
//        }
//    }

//    getConnection() {
//        return this.connection;
//    }
//    // Invoke a method on the server
//    async invoke(methodName, ...args) {
//        try {
//            return await this.connection.invoke(methodName, ...args);
//        } catch (error) {
//            console.error(`Error invoking SignalR method '${methodName}':`, error);
//            throw error;
//        }
//    }
//}
