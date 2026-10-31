import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#4f5fbd', dark: '#394994', light: '#edf0ff' },
    secondary: { main: '#159a8c' },
    background: { default: '#f6f7f9', paper: '#ffffff' },
    text: { primary: '#20243a', secondary: '#6d7286' },
    success: { main: '#16866b' },
    error: { main: '#c43f55' },
  },
  shape: { borderRadius: 10 },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h1: { fontSize: '1.9rem', fontWeight: 730, letterSpacing: '-0.035em' },
    h2: { fontSize: '1.5rem', fontWeight: 720, letterSpacing: '-0.03em' },
    h3: { fontSize: '1.15rem', fontWeight: 700 },
    button: { textTransform: 'none', fontWeight: 650 },
  },
  components: {
    MuiCard: { styleOverrides: { root: { border: '1px solid #e5e7ec', boxShadow: 'none', borderRadius: 10 } } },
    MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { borderRadius: 10, padding: '9px 15px' } } },
    MuiTextField: { defaultProps: { size: 'medium', fullWidth: true } },
  },
});
