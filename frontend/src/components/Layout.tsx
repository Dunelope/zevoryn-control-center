import { NavLink, Outlet } from 'react-router-dom';
const links = [['/', 'Dashboard'], ['/products', 'Products'], ['/events', 'Events'], ['/monitoring', 'Monitoring'], ['/beta', 'Beta'], ['/customers', 'Customers'], ['/billing', 'Billing'], ['/ai-lab', 'AI Lab']];
export function Layout() { return <div className="app-shell"><aside><h1>Zevoryn</h1><p className="muted">Control Center</p><nav>{links.map(([to, label]) => <NavLink key={to} to={to} end={to === '/'}>{label}</NavLink>)}</nav></aside><main><Outlet /></main></div>; }
