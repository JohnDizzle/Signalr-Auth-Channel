import { ConnectionManager } from './connectionManager.js';

let instance = null;

export function getConnectionManager(hubUrl) {
    if (!instance) {
        instance = new ConnectionManager(hubUrl);
    }
    return instance;
}