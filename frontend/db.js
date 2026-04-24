const DB_NAME = "subscriptionTrackerDB";
const DB_VERSION = 1;
const SUBS_STORE = "subscriptions";
const OPS_STORE = "pendingOps";

function openDB() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);

    req.onupgradeneeded = () => {
      const db = req.result;

      if (!db.objectStoreNames.contains(SUBS_STORE)) {
        db.createObjectStore(SUBS_STORE, { keyPath: "id" });
      }

      if (!db.objectStoreNames.contains(OPS_STORE)) {
        db.createObjectStore(OPS_STORE, { keyPath: "id" });
      }
    };

    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function withStore(storeName, mode, fn) {
  const db = await openDB();

  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, mode);
    const store = tx.objectStore(storeName);

    Promise.resolve(fn(store))
      .then((result) => {
        tx.oncomplete = () => resolve(result);
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error);
      })
      .catch(reject);
  });
}

function readAllFrom(store) {
  return new Promise((resolve, reject) => {
    const req = store.getAll();
    req.onsuccess = () => resolve(req.result || []);
    req.onerror = () => reject(req.error);
  });
}

function putIn(store, value) {
  return new Promise((resolve, reject) => {
    const req = store.put(value);
    req.onsuccess = () => resolve(true);
    req.onerror = () => reject(req.error);
  });
}

function deleteFrom(store, id) {
  return new Promise((resolve, reject) => {
    const req = store.delete(id);
    req.onsuccess = () => resolve(true);
    req.onerror = () => reject(req.error);
  });
}

function clearStore(store) {
  return new Promise((resolve, reject) => {
    const req = store.clear();
    req.onsuccess = () => resolve(true);
    req.onerror = () => reject(req.error);
  });
}

export async function dbGetAllSubscriptions() {
  return withStore(SUBS_STORE, "readonly", readAllFrom);
}

export async function dbUpsertSubscription(subscription) {
  return withStore(SUBS_STORE, "readwrite", (store) => putIn(store, subscription));
}

export async function dbDeleteSubscription(id) {
  return withStore(SUBS_STORE, "readwrite", (store) => deleteFrom(store, id));
}

export async function dbClearSubscriptions() {
  return withStore(SUBS_STORE, "readwrite", clearStore);
}

export async function dbGetAllPendingOps() {
  return withStore(OPS_STORE, "readonly", readAllFrom);
}

export async function dbUpsertPendingOp(operation) {
  return withStore(OPS_STORE, "readwrite", (store) => putIn(store, operation));
}

export async function dbDeletePendingOp(id) {
  return withStore(OPS_STORE, "readwrite", (store) => deleteFrom(store, id));
}

export async function dbClearPendingOps() {
  return withStore(OPS_STORE, "readwrite", clearStore);
}