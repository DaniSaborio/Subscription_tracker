const API_BASE = "http://localhost:5000";

async function parseResponse(response) {
  if (!response.ok) {
    let message = `HTTP ${response.status}`;
    try {
      const errorBody = await response.json();
      if (errorBody?.message) {
        message = errorBody.message;
      }
    } catch {
      // Ignore JSON parse failures.
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return null;
  }

  return await response.json();
}

async function authorizedFetch(url, token, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {}),
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  return parseResponse(response);
}

export async function apiRegister(email, password) {
  return authorizedFetch(`${API_BASE}/auth/register`, null, {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function apiLogin(email, password) {
  return authorizedFetch(`${API_BASE}/auth/login`, null, {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function apiGetSubscriptions(token) {
  return authorizedFetch(`${API_BASE}/subscriptions`, token, { method: "GET" });
}

export async function apiCreateSubscription(token, payload) {
  return authorizedFetch(`${API_BASE}/subscriptions`, token, {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function apiUpdateSubscription(token, id, payload) {
  return authorizedFetch(`${API_BASE}/subscriptions/${id}`, token, {
    method: "PUT",
    body: JSON.stringify(payload),
  });
}

export async function apiDeleteSubscription(token, id) {
  return authorizedFetch(`${API_BASE}/subscriptions/${id}`, token, {
    method: "DELETE",
  });
}

export async function apiShareSubscription(token, id, email) {
  return authorizedFetch(`${API_BASE}/subscriptions/${id}/share`, token, {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export async function apiRevokeSubscriptionShare(token, id, email) {
  const encodedEmail = encodeURIComponent(email);
  return authorizedFetch(`${API_BASE}/subscriptions/${id}/share?email=${encodedEmail}`, token, {
    method: "DELETE",
  });
}

export async function apiGetSubscriptionShares(token, id) {
  return authorizedFetch(`${API_BASE}/subscriptions/${id}/shares`, token, {
    method: "GET",
  });
}

export async function apiGetSummary(token) {
  return authorizedFetch(`${API_BASE}/subscriptions/summary`, token, {
    method: "GET",
  });
}