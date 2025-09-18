import './index.css';
import './styles.css';
import 'react-toastify/dist/ReactToastify.css';

import { CssBaseline } from '@mui/material';

import App from './App';
import React from 'react';
import ReactDOM from 'react-dom/client';
import reportWebVitals from './reportWebVitals';

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <CssBaseline />
    <App />
  </React.StrictMode>
);

reportWebVitals();