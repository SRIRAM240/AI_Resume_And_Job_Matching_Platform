import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  AppBar, Avatar, Box, Button, Divider, Drawer, IconButton, List, ListItemButton,
  ListItemIcon, ListItemText, Toolbar, Typography, useMediaQuery, useTheme,
} from '@mui/material';
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded';
import DescriptionRoundedIcon from '@mui/icons-material/DescriptionRounded';
import WorkOutlineRoundedIcon from '@mui/icons-material/WorkOutlineRounded';
import CompareArrowsRoundedIcon from '@mui/icons-material/CompareArrowsRounded';
import PersonOutlineRoundedIcon from '@mui/icons-material/PersonOutlineRounded';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import { useAuth } from '../auth/AuthContext';

const drawerWidth = 252;
const navItems = [
  { label: 'Overview', to: '/', icon: <DashboardRoundedIcon /> },
  { label: 'My resumes', to: '/resumes', icon: <DescriptionRoundedIcon /> },
  { label: 'Job descriptions', to: '/jobs', icon: <WorkOutlineRoundedIcon /> },
  { label: 'Compare jobs', to: '/compare', icon: <CompareArrowsRoundedIcon /> },
  { label: 'My profile', to: '/profile', icon: <PersonOutlineRoundedIcon /> },
];

export default function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Box sx={{ px: 2.5, py: 2.6, display: 'flex', alignItems: 'center', gap: 1.3 }}>
        <Box className="brand-mark"><AutoAwesomeRoundedIcon /></Box>
        <Box>
          <Typography fontWeight={800} letterSpacing="-0.03em">ATS Resume Checker</Typography>
          <Typography variant="caption" color="text.secondary">Resume intelligence</Typography>
        </Box>
      </Box>
      <Divider />
      <Typography variant="overline" sx={{ px: 2.7, pt: 3, pb: 1, color: 'text.secondary', fontWeight: 700 }}>WORKSPACE</Typography>
      <List sx={{ px: 1.2 }}>
        {navItems.map((item) => (
          <ListItemButton key={item.to} component={NavLink} to={item.to} end={item.to === '/'}
            onClick={() => setMobileOpen(false)}
            sx={{
              borderRadius: 1.5, mb: .35, py: 1.05, color: 'text.secondary',
              '&.active': { bgcolor: '#efedff', color: 'primary.main', '& .MuiListItemIcon-root': { color: 'primary.main' } },
            }}>
            <ListItemIcon sx={{ minWidth: 39, color: 'inherit' }}>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14, fontWeight: 650 }} />
          </ListItemButton>
        ))}
      </List>
      <Box sx={{ mt: 'auto', p: 2 }}>
        <Divider sx={{ mb: 1.5 }} />
        <Box sx={{ display: 'flex', gap: 1.2, alignItems: 'center' }}>
          <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main' }}>{user?.fullName?.[0]?.toUpperCase() ?? 'U'}</Avatar>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="body2" fontWeight={700} noWrap>{user?.fullName ?? 'User'}</Typography>
            <Typography variant="caption" color="text.secondary" noWrap>{user?.email ?? ''}</Typography>
          </Box>
          <IconButton size="small" aria-label="Log out" onClick={() => { logout(); navigate('/login'); }}><LogoutRoundedIcon fontSize="small" /></IconButton>
        </Box>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AppBar position="fixed" color="inherit" elevation={0} sx={{ borderBottom: '1px solid #e9ebf3', bgcolor: 'rgba(255,255,255,.92)', backdropFilter: 'blur(14px)', width: { md: `calc(100% - ${drawerWidth}px)` }, ml: { md: `${drawerWidth}px` } }}>
        <Toolbar sx={{ minHeight: '72px !important', gap: 2 }}>
          {isMobile && <IconButton onClick={() => setMobileOpen(true)}><MenuRoundedIcon /></IconButton>}
          <Box sx={{ flex: 1 }}>
          
          </Box>
          <Button color="inherit" startIcon={<LogoutRoundedIcon />} onClick={() => { logout(); navigate('/login'); }} sx={{ display: { xs: 'none', sm: 'inline-flex' } }}>Sign out</Button>
        </Toolbar>
      </AppBar>
      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer variant="temporary" open={mobileOpen} onClose={() => setMobileOpen(false)} ModalProps={{ keepMounted: true }} sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' } }}>{drawerContent}</Drawer>
        <Drawer variant="permanent" open sx={{ display: { xs: 'none', md: 'block' }, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', borderRight: '1px solid #e9ebf3', bgcolor: '#fff' } }}>{drawerContent}</Drawer>
      </Box>
      <Box component="main" sx={{ ml: { md: `${drawerWidth}px` }, pt: '72px', minHeight: '100vh' }}>
        <Outlet />
      </Box>
    </Box>
  );
}
