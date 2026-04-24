import {
  applyFilters,
  createSubscription,
  deleteSubscription,
  getPendingSummary,
  getSessionState,
  getSummary,
  initSubscriptions,
  login,
  logout,
  refreshFromServer,
  register,
  syncPending,
  updateSubscription,
} from "./taskService.js";

const netStatus = document.getElementById("netStatus");

const authSection = document.getElementById("authSection");
const appSection = document.getElementById("appSection");
const emailInput = document.getElementById("emailInput");
const passwordInput = document.getElementById("passwordInput");
const loginBtn = document.getElementById("loginBtn");
const registerBtn = document.getElementById("registerBtn");
const logoutBtn = document.getElementById("logoutBtn");
const authStatus = document.getElementById("authStatus");

const nameInput = document.getElementById("nameInput");
const categoryInput = document.getElementById("categoryInput");
const cycleInput = document.getElementById("cycleInput");
const amountInput = document.getElementById("amountInput");
const currencyInput = document.getElementById("currencyInput");
const nextBillingInput = document.getElementById("nextBillingInput");
const notesInput = document.getElementById("notesInput");
const addBtn = document.getElementById("addBtn");

const searchInput = document.getElementById("searchInput");
const filterCategoryInput = document.getElementById("filterCategoryInput");
const filterCycleInput = document.getElementById("filterCycleInput");

const monthlyTotal = document.getElementById("monthlyTotal");
const yearlyTotal = document.getElementById("yearlyTotal");
const upcomingCount = document.getElementById("upcomingCount");
const cycleTotals = document.getElementById("cycleTotals");
const syncBtn = document.getElementById("syncBtn");
const syncStatus = document.getElementById("syncStatus");
const list = document.getElementById("list");

function fmtMoney(value, currency = "USD") {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
  }).format(value || 0);
}

function updateNetworkStatus() {
  const online = navigator.onLine;
  netStatus.textContent = online ? "Online" : "Offline";
  netStatus.classList.toggle("online", online);
  netStatus.classList.toggle("offline", !online);
}

function getActiveFilters() {
  return {
    search: searchInput.value,
    category: filterCategoryInput.value,
    billingCycle: filterCycleInput.value,
  };
}

async function renderSummary() {
  const summary = await getSummary();
  monthlyTotal.textContent = fmtMoney(summary.monthlyEquivalentTotal, "USD");
  yearlyTotal.textContent = fmtMoney(summary.yearlyEquivalentTotal, "USD");
  upcomingCount.textContent = String(summary.upcomingIn30Days || 0);

  const parts = Object.entries(summary.totalsByBillingCycle || {}).map(([cycle, total]) => {
    return `${cycle}: ${fmtMoney(total, "USD")}`;
  });

  cycleTotals.textContent = `Totales por ciclo: ${parts.length ? parts.join(" | ") : "-"}`;
}

function buildRow(item) {
  const soon = (() => {
    const now = new Date();
    const due = new Date(item.nextBillingDate);
    const days = Math.ceil((due - now) / 86400000);
    if (days < 0) return "Vencida";
    if (days <= 7) return `Vence en ${days} dias`;
    return "";
  })();

  const card = document.createElement("article");
  card.className = "subscription-card";

  const pendingBadge = item.synced === false ? '<span class="pending-badge">Pendiente de sync</span>' : "";

  card.innerHTML = `
    <div class="subscription-top">
      <p class="subscription-name">${item.name}</p>
      <p>${fmtMoney(item.amount, item.currency)} / ${item.billingCycle}</p>
    </div>
    <p class="subscription-meta">
      Categoria: ${item.category} | Proximo cobro: ${item.nextBillingDate} ${soon ? `| ${soon}` : ""}
    </p>
    <p class="subscription-meta">${item.notes || "Sin notas"}</p>
    ${pendingBadge}
    <div class="subscription-actions">
      <button class="ghost" data-action="edit">Editar</button>
      <button class="danger" data-action="delete">Eliminar</button>
    </div>
  `;

  card.querySelector('[data-action="edit"]').addEventListener("click", async () => {
    const nextName = prompt("Nombre", item.name);
    if (!nextName) return;

    const nextCategory = prompt("Categoria", item.category);
    if (!nextCategory) return;

    const nextAmountText = prompt("Monto", String(item.amount));
    if (!nextAmountText) return;

    const nextDate = prompt("Proximo cobro (YYYY-MM-DD)", item.nextBillingDate);
    if (!nextDate) return;

    try {
      await updateSubscription(item.id, {
        name: nextName,
        category: nextCategory,
        billingCycle: item.billingCycle,
        amount: Number(nextAmountText),
        currency: item.currency,
        nextBillingDate: nextDate,
        notes: item.notes,
      });
      await renderAll();
    } catch (error) {
      syncStatus.textContent = `No se pudo editar: ${error.message}`;
    }
  });

  card.querySelector('[data-action="delete"]').addEventListener("click", async () => {
    if (!confirm(`Eliminar ${item.name}?`)) return;

    await deleteSubscription(item.id);
    await renderAll();
  });

  return card;
}

async function renderList() {
  const filtered = applyFilters(getActiveFilters());
  list.innerHTML = "";

  if (!filtered.length) {
    list.innerHTML = '<p class="status-text">No hay suscripciones para este filtro.</p>';
    return;
  }

  for (const item of filtered) {
    list.appendChild(buildRow(item));
  }
}

async function updateSyncLabel() {
  const pending = await getPendingSummary();
  syncStatus.textContent = pending.total
    ? `Hay ${pending.total} cambios pendientes de sincronizar.`
    : "Sincronizado.";
}

async function renderAll() {
  await renderSummary();
  await renderList();
  await updateSyncLabel();
}

function setAuthUi(loggedIn, email = "") {
  authSection.classList.toggle("hidden", loggedIn);
  appSection.classList.toggle("hidden", !loggedIn);

  if (loggedIn) {
    authStatus.textContent = `Sesion activa: ${email}`;
  }
}

async function handleAuth(action) {
  const email = emailInput.value.trim();
  const password = passwordInput.value.trim();

  if (!email || !password) {
    authStatus.textContent = "Debes completar email y password.";
    return;
  }

  try {
    if (action === "login") {
      await login(email, password);
    } else {
      await register(email, password);
    }

    await initSubscriptions();

    if (navigator.onLine) {
      await refreshFromServer();
    }

    const session = getSessionState();
    setAuthUi(true, session.email);
    await renderAll();
  } catch (error) {
    authStatus.textContent = `Error de autenticacion: ${error.message}`;
  }
}

async function handleAdd() {
  const payload = {
    name: nameInput.value.trim(),
    category: categoryInput.value.trim(),
    billingCycle: cycleInput.value,
    amount: Number(amountInput.value),
    currency: (currencyInput.value || "USD").trim().toUpperCase(),
    nextBillingDate: nextBillingInput.value,
    notes: notesInput.value.trim(),
  };

  if (!payload.name || !payload.category || !payload.nextBillingDate || payload.amount <= 0) {
    syncStatus.textContent = "Completa nombre, categoria, monto y proximo cobro.";
    return;
  }

  await createSubscription(payload);

  nameInput.value = "";
  categoryInput.value = "";
  amountInput.value = "";
  notesInput.value = "";

  await renderAll();
}

async function handleManualSync() {
  if (!navigator.onLine) {
    syncStatus.textContent = "Sin internet. La sincronizacion requiere conexion.";
    return;
  }

  const result = await syncPending();
  await refreshFromServer();
  await renderAll();
  syncStatus.textContent = `Sync completado: ${result.success}/${result.processed}.`;
}

function bindEvents() {
  loginBtn.addEventListener("click", async () => handleAuth("login"));
  registerBtn.addEventListener("click", async () => handleAuth("register"));
  logoutBtn.addEventListener("click", async () => {
    await logout();
    setAuthUi(false);
    authStatus.textContent = "Sesion cerrada.";
  });

  addBtn.addEventListener("click", handleAdd);
  syncBtn.addEventListener("click", handleManualSync);

  searchInput.addEventListener("input", renderList);
  filterCategoryInput.addEventListener("input", renderList);
  filterCycleInput.addEventListener("change", renderList);

  window.addEventListener("online", async () => {
    updateNetworkStatus();
    try {
      const session = getSessionState();
      if (!session.token) return;

      const result = await syncPending();
      await refreshFromServer();
      await renderAll();
      syncStatus.textContent = `Conexion recuperada. Sync: ${result.success}/${result.processed}.`;
    } catch (error) {
      syncStatus.textContent = `No se pudo sincronizar automaticamente: ${error.message}`;
    }
  });

  window.addEventListener("offline", () => {
    updateNetworkStatus();
    syncStatus.textContent = "Modo offline activo. Los cambios quedan pendientes.";
  });
}

export async function initUI() {
  updateNetworkStatus();
  bindEvents();

  const session = getSessionState();
  if (!session.token) {
    setAuthUi(false);
    return;
  }

  await initSubscriptions();
  setAuthUi(true, session.email);

  if (navigator.onLine) {
    try {
      await refreshFromServer();
    } catch (error) {
      syncStatus.textContent = `No se pudo actualizar desde servidor: ${error.message}`;
    }
  }

  await renderAll();
}
