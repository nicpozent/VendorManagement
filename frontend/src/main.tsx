import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { MsalProvider } from "@azure/msal-react";
import { pca } from "./auth/msal";
import { AppProvider } from "./app/AppProvider";
import App from "./App";
import "./theme/styles.css";

async function bootstrap() {
  if (pca) await pca.initialize();

  const tree = (
    <BrowserRouter>
      <AppProvider>
        <App />
      </AppProvider>
    </BrowserRouter>
  );

  const rooted = pca ? <MsalProvider instance={pca}>{tree}</MsalProvider> : tree;
  ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>{rooted}</React.StrictMode>,
  );
}

void bootstrap();
