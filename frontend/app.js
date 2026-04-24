// app.js
// Punto de entrada de la aplicación.

import { initUI } from "./ui.js";

console.log("Subscription Tracker PWA iniciada");

if ("serviceWorker" in navigator) {
  navigator.serviceWorker
    .register("./service-worker.js")
    .then((reg) => console.log("SW REGISTRADO. Scope:", reg.scope))
    .catch((err) => console.error("SW FALLÓ:", err));
}

await initUI();