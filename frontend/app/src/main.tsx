import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { Provider } from "react-redux";
import { LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { store } from "./store";
import { ApiReadyGate } from "./components/ApiReadyGate";
import App from "./App.tsx";
import "./index.css";
import "./styles/animations.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Provider store={store}>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <BrowserRouter>
          <ApiReadyGate>
            <App />
          </ApiReadyGate>
        </BrowserRouter>
      </LocalizationProvider>
    </Provider>
  </StrictMode>
);
