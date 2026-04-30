import {
  apiCreateSubscription,
  apiDeleteSubscription,
  apiGetSubscriptions,
  apiGetSummary,
  apiLogin,
  apiRegister,
  apiRevokeSubscriptionShare,
  apiShareSubscription,
  apiUpdateSubscription,
  apiGetSubscriptionShares,
} from "./api.js";
import {
  dbClearPendingOps,
  dbClearSubscriptions,
  dbDeletePendingOp,
  dbDeleteSubscription,
  dbGetAllPendingOps,
  dbGetAllSubscriptions,
  dbUpsertPendingOp,
  dbUpsertSubscription,
} from "./db.js";

const AUTH_TOKEN_KEY = "subs_token";
const AUTH_EMAIL_KEY = "subs_email";

let subscriptions = [];

function uid() {
  return `local_${Date.now().toString(36)}_${Math.random().toString(36).slice(2)}`;
}

function normalizeSubscription(raw, isSynced = true) {
  return {
    id: String(raw.id),
    name: String(raw.name || ""),
    category: String(raw.category || "Other"),
    billingCycle: String(raw.billingCycle || "monthly").toLowerCase(),
    amount: Number(raw.amount || 0),
    currency: String(raw.currency || "USD").toUpperCase(),
    nextBillingDate: String(raw.nextBillingDate || new Date().toISOString().slice(0, 10)).slice(0, 10),
    notes: raw.notes || "",
    isOwner: raw.isOwner !== false,
    sharedByEmail: raw.sharedByEmail || "",
    updatedAt: raw.updatedAt ? new Date(raw.updatedAt).getTime() : Date.now(),
    synced: isSynced,
  };
}

function sortSubscriptions() {
  subscriptions.sort((a, b) => {
    if (a.nextBillingDate === b.nextBillingDate) {
      return (b.updatedAt || 0) - (a.updatedAt || 0);
    }

    return a.nextBillingDate.localeCompare(b.nextBillingDate);
  });
}

function getAuthToken() {
  return localStorage.getItem(AUTH_TOKEN_KEY) || "";
}

function getAuthEmail() {
  return localStorage.getItem(AUTH_EMAIL_KEY) || "";
}

function setAuth(authResponse) {
  localStorage.setItem(AUTH_TOKEN_KEY, authResponse.token);
  localStorage.setItem(AUTH_EMAIL_KEY, authResponse.email);
}

function queueOperation(operation) {
  return dbUpsertPendingOp(operation);
}

function buildPayload(subscription) {
  return {
    name: subscription.name,
    category: subscription.category,
    billingCycle: subscription.billingCycle,
    amount: subscription.amount,
    currency: subscription.currency,
    nextBillingDate: subscription.nextBillingDate,
    notes: subscription.notes || null,
  };
}

export function getSessionState() {
  return {
    token: getAuthToken(),
    email: getAuthEmail(),
  };
}

export async function login(email, password) {
  const auth = await apiLogin(email, password);
  setAuth(auth);
  return auth;
}

export async function register(email, password) {
  const auth = await apiRegister(email, password);
  setAuth(auth);
  return auth;
}

export async function logout() {
  localStorage.removeItem(AUTH_TOKEN_KEY);
  localStorage.removeItem(AUTH_EMAIL_KEY);
  subscriptions = [];
  await dbClearSubscriptions();
  await dbClearPendingOps();
}

export async function initSubscriptions() {
  const stored = await dbGetAllSubscriptions();
  subscriptions = stored.map(item => normalizeSubscription(item, item.synced !== false));
  sortSubscriptions();
  return [...subscriptions];
}

export function getSubscriptions() {
  return [...subscriptions];
}

export async function refreshFromServer() {
  const token = getAuthToken();
  const fromApi = await apiGetSubscriptions(token);

  subscriptions = fromApi.map((item) => normalizeSubscription(item, true));
  await dbClearSubscriptions();
  for (const item of subscriptions) {
    await dbUpsertSubscription(item);
  }

  sortSubscriptions();
  return [...subscriptions];
}

export async function createSubscription(payload) {
  const token = getAuthToken();
  const localItem = normalizeSubscription(
    {
      ...payload,
      id: uid(),
      updatedAt: new Date().toISOString(),
    },
    false
  );

  if (navigator.onLine) {
    try {
      const created = await apiCreateSubscription(token, buildPayload(localItem));
      const normalized = normalizeSubscription(created, true);
      subscriptions.unshift(normalized);
      await dbUpsertSubscription(normalized);
      sortSubscriptions();
      return normalized;
    } catch {
      // Fall back to queue mode.
    }
  }

  subscriptions.unshift(localItem);
  await dbUpsertSubscription(localItem);
  await queueOperation({
    id: `create:${localItem.id}`,
    type: "create",
    subscriptionId: localItem.id,
    payload: buildPayload(localItem),
    createdAt: Date.now(),
  });
  sortSubscriptions();
  return localItem;
}

export async function updateSubscription(id, payload) {
  const token = getAuthToken();
  const current = subscriptions.find((x) => x.id === id);
  if (!current) {
    throw new Error("Suscripcion no encontrada.");
  }

  const updatedLocal = {
    ...current,
    ...payload,
    amount: Number(payload.amount),
    updatedAt: Date.now(),
    synced: false,
  };

  if (navigator.onLine && !String(id).startsWith("local_")) {
    try {
      const updated = await apiUpdateSubscription(token, id, buildPayload(updatedLocal));
      const normalized = normalizeSubscription(updated, true);
      subscriptions = subscriptions.map((x) => (x.id === id ? normalized : x));
      await dbUpsertSubscription(normalized);
      sortSubscriptions();
      return normalized;
    } catch {
      // Fall back to queue mode.
    }
  }

  subscriptions = subscriptions.map((x) => (x.id === id ? updatedLocal : x));
  await dbUpsertSubscription(updatedLocal);

  if (String(id).startsWith("local_")) {
    await queueOperation({
      id: `create:${id}`,
      type: "create",
      subscriptionId: id,
      payload: buildPayload(updatedLocal),
      createdAt: Date.now(),
    });
  } else {
    await queueOperation({
      id: `update:${id}`,
      type: "update",
      subscriptionId: id,
      payload: buildPayload(updatedLocal),
      createdAt: Date.now(),
    });
  }

  sortSubscriptions();
  return updatedLocal;
}

export async function deleteSubscription(id) {
  const token = getAuthToken();
  const isLocal = String(id).startsWith("local_");

  subscriptions = subscriptions.filter((x) => x.id !== id);
  await dbDeleteSubscription(id);

  if (isLocal) {
    await dbDeletePendingOp(`create:${id}`);
    return;
  }

  if (navigator.onLine) {
    try {
      await apiDeleteSubscription(token, id);
      await dbDeletePendingOp(`update:${id}`);
      await dbDeletePendingOp(`delete:${id}`);
      return;
    } catch {
      // Fall back to queue mode.
    }
  }

  await dbDeletePendingOp(`update:${id}`);
  await queueOperation({
    id: `delete:${id}`,
    type: "delete",
    subscriptionId: id,
    createdAt: Date.now(),
  });
}

export async function shareSubscription(id, email) {
  const token = getAuthToken();
  if (!navigator.onLine) {
    throw new Error("La comparticion requiere conexion.");
  }

  await apiShareSubscription(token, id, email);
}

export async function revokeSubscriptionShare(id, email) {
  const token = getAuthToken();
  if (!navigator.onLine) {
    throw new Error("La comparticion requiere conexion.");
  }

  await apiRevokeSubscriptionShare(token, id, email);
}

export async function getSubscriptionShares(id) {
  const token = getAuthToken();
  if (!navigator.onLine) {
    throw new Error("La operación requiere conexión.");
  }

  return await apiGetSubscriptionShares(token, id);
}

export async function syncPending() {
  const token = getAuthToken();
  const pendingOps = await dbGetAllPendingOps();
  const ordered = [...pendingOps].sort((a, b) => (a.createdAt || 0) - (b.createdAt || 0));

  let success = 0;
  let failed = 0;

  for (const op of ordered) {
    try {
      if (op.type === "create") {
        const created = await apiCreateSubscription(token, op.payload);
        const normalized = normalizeSubscription(created, true);

        subscriptions = subscriptions.map((x) => (x.id === op.subscriptionId ? normalized : x));
        await dbDeleteSubscription(op.subscriptionId);
        await dbUpsertSubscription(normalized);
      }

      if (op.type === "update") {
        const updated = await apiUpdateSubscription(token, op.subscriptionId, op.payload);
        const normalized = normalizeSubscription(updated, true);
        subscriptions = subscriptions.map((x) => (x.id === op.subscriptionId ? normalized : x));
        await dbUpsertSubscription(normalized);
      }

      if (op.type === "delete") {
        await apiDeleteSubscription(token, op.subscriptionId);
      }

      await dbDeletePendingOp(op.id);
      success += 1;
    } catch {
      failed += 1;
    }
  }

  sortSubscriptions();

  return {
    processed: ordered.length,
    success,
    failed,
  };
}

function monthlyEquivalent(sub) {
  switch (sub.billingCycle) {
    case "weekly":
      return (sub.amount * 52) / 12;
    case "biweekly":
      return (sub.amount * 26) / 12;
    case "quarterly":
      return sub.amount / 3;
    case "yearly":
      return sub.amount / 12;
    default:
      return sub.amount;
  }
}

export async function getSummary() {
  const token = getAuthToken();

  if (navigator.onLine && token) {
    try {
      return await apiGetSummary(token);
    } catch {
      // Ignore and use local summary.
    }
  }

  const totalsByBillingCycle = subscriptions.reduce((acc, item) => {
    acc[item.billingCycle] = (acc[item.billingCycle] || 0) + item.amount;
    return acc;
  }, {});

  const monthly = subscriptions.reduce((sum, item) => sum + monthlyEquivalent(item), 0);
  const now = new Date();
  const limit = new Date(now);
  limit.setDate(limit.getDate() + 30);

  const upcomingIn30Days = subscriptions.filter((item) => {
    const due = new Date(item.nextBillingDate);
    return due >= now && due <= limit;
  }).length;

  return {
    monthlyEquivalentTotal: Number(monthly.toFixed(2)),
    yearlyEquivalentTotal: Number((monthly * 12).toFixed(2)),
    totalsByBillingCycle,
    upcomingIn30Days,
  };
}

export async function getPendingSummary() {
  const pendingOps = await dbGetAllPendingOps();
  return {
    total: pendingOps.length,
  };
}

export function applyFilters(filters) {
  const search = (filters.search || "").trim().toLowerCase();
  const category = (filters.category || "").trim().toLowerCase();
  const cycle = (filters.billingCycle || "").trim().toLowerCase();

  return subscriptions.filter((item) => {
    const matchesSearch = !search || item.name.toLowerCase().includes(search) || item.category.toLowerCase().includes(search);
    const matchesCategory = !category || item.category.toLowerCase().includes(category);
    const matchesCycle = !cycle || item.billingCycle === cycle;
    return matchesSearch && matchesCategory && matchesCycle;
  });
}
