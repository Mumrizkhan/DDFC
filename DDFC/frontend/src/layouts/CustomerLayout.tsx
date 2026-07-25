import React, { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Plus,
  CreditCard,
  MessageSquare,
  Bell,
  User,
  LogOut,
  Menu,
  X,
} from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { customerLogout } from '../store/slices/authSlice';

const navItems = [
  { path: '/portal', label: 'My Requests', icon: LayoutDashboard, end: true },
  { path: '/portal/new-request', label: 'New Request', icon: Plus },
  { path: '/portal/payments', label: 'Payments', icon: CreditCard },
  { path: '/portal/tickets', label: 'Support', icon: MessageSquare },
  { path: '/portal/notifications', label: 'Notifications', icon: Bell },
  { path: '/portal/profile', label: 'Profile', icon: User },
];

export const CustomerLayout: React.FC = () => {
  const [menuOpen, setMenuOpen] = useState(false);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const customer = useAppSelector((s) => s.auth.customer);
  const unreadCount = useAppSelector((s) => s.notifications.unreadCount);

  const handleLogout = () => {
    dispatch(customerLogout());
    navigate('/portal/login');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top navigation */}
      <header className="bg-white shadow-sm sticky top-0 z-10">
        <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
          {/* Logo */}
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-green-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xs">D</span>
            </div>
            <div>
              <p className="font-bold text-gray-900 text-sm leading-tight">DDFC</p>
              <p className="text-gray-400 text-xs">DHA Peshawar</p>
            </div>
          </div>

          {/* Desktop nav */}
          <nav className="hidden md:flex items-center gap-1">
            {navItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.end}
                className={({ isActive }) =>
                  `relative flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors
                  ${isActive
                    ? 'bg-green-50 text-green-700 font-medium'
                    : 'text-gray-600 hover:bg-gray-100'}`
                }
              >
                <item.icon size={16} />
                {item.label}
                {item.path === '/portal/notifications' && unreadCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center">
                    {unreadCount}
                  </span>
                )}
              </NavLink>
            ))}
          </nav>

          {/* User + logout */}
          <div className="flex items-center gap-3">
            <div className="hidden md:flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-green-100 flex items-center justify-center">
                <span className="text-green-700 text-sm font-medium">
                  {customer?.fullName?.charAt(0) ?? 'C'}
                </span>
              </div>
              <span className="text-sm text-gray-700 font-medium">
                {customer?.fullName}
              </span>
            </div>
            <button
              onClick={handleLogout}
              className="hidden md:flex items-center gap-1 text-gray-500 hover:text-red-600 text-sm transition-colors"
            >
              <LogOut size={16} />
            </button>
            {/* Mobile menu button */}
            <button
              className="md:hidden text-gray-500"
              onClick={() => setMenuOpen((o) => !o)}
            >
              {menuOpen ? <X size={20} /> : <Menu size={20} />}
            </button>
          </div>
        </div>

        {/* Mobile menu */}
        {menuOpen && (
          <div className="md:hidden border-t border-gray-100 bg-white px-4 py-2 space-y-1">
            {navItems.map((item) => (
              <NavLink
                key={item.path}
                to={item.path}
                end={item.end}
                className={({ isActive }) =>
                  `flex items-center gap-2 px-3 py-2.5 rounded-lg text-sm
                  ${isActive ? 'bg-green-50 text-green-700 font-medium' : 'text-gray-600'}`
                }
                onClick={() => setMenuOpen(false)}
              >
                <item.icon size={16} />
                {item.label}
              </NavLink>
            ))}
            <button
              onClick={handleLogout}
              className="flex items-center gap-2 px-3 py-2.5 text-sm text-red-600 w-full"
            >
              <LogOut size={16} />
              Logout
            </button>
          </div>
        )}
      </header>

      {/* Page content */}
      <main className="max-w-6xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  );
};
