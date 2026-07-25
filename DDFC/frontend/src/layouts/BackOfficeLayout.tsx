import React, { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  ClipboardList,
  CheckSquare,
  Headphones,
  Bell,
  LogOut,
  Menu,
  CalendarDays,
} from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { staffLogout } from '../store/slices/authSlice';

export const BackOfficeLayout: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const user = useAppSelector((s) => s.auth.staffUser);

  const isTechSupport = user?.roleName === 'Technical Support';

  const navItems = isTechSupport
    ? [
        { path: '/backoffice', label: 'Dashboard',      icon: LayoutDashboard, end: true },
        { path: '/backoffice/tickets', label: 'Tickets Queue', icon: Headphones },
      ]
    : [
        { path: '/backoffice', label: 'Dashboard',     icon: LayoutDashboard, end: true },
        { path: '/backoffice/requests',     label: 'Requests',    icon: ClipboardList },
        { path: '/backoffice/tasks',        label: 'My Tasks',    icon: CheckSquare },
        { path: '/backoffice/appointments', label: 'Appointments', icon: CalendarDays },
      ];

  const handleLogout = () => {
    dispatch(staffLogout());
    navigate('/staff/login');
  };

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-30 w-64 bg-slate-800 transform transition-transform lg:static lg:translate-x-0
          ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}`}
      >
        <div className="flex items-center gap-3 px-6 py-5 border-b border-slate-700">
          <div className="w-9 h-9 bg-emerald-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-sm">D</span>
          </div>
          <div>
            <p className="text-white font-bold text-sm">DDFC</p>
            <p className="text-slate-400 text-xs">
              {user?.departmentName ?? 'Back Office'}
            </p>
          </div>
        </div>

        <nav className="px-3 py-4 space-y-0.5">
          {navItems.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.end}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors
                ${isActive
                  ? 'bg-emerald-600 text-white'
                  : 'text-slate-400 hover:bg-slate-700 hover:text-white'}`
              }
              onClick={() => setSidebarOpen(false)}
            >
              <item.icon size={18} />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="absolute bottom-0 left-0 right-0 px-4 py-4 border-t border-slate-700">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-emerald-500 flex items-center justify-center">
              <span className="text-white text-sm font-medium">
                {user?.fullName?.charAt(0) ?? 'S'}
              </span>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-white text-sm font-medium truncate">{user?.fullName}</p>
              <p className="text-slate-400 text-xs truncate">{user?.roleName}</p>
            </div>
            <button
              onClick={handleLogout}
              className="text-slate-400 hover:text-white"
              title="Logout"
            >
              <LogOut size={16} />
            </button>
          </div>
        </div>
      </aside>

      <div className="flex flex-col flex-1 overflow-hidden">
        <header className="bg-white border-b border-gray-200 px-4 py-3 flex items-center gap-3">
          <button
            className="lg:hidden text-gray-500 hover:text-gray-700"
            onClick={() => setSidebarOpen(true)}
          >
            <Menu size={20} />
          </button>
          <div className="flex-1" />
          <button className="text-gray-500 hover:text-gray-700">
            <Bell size={20} />
          </button>
        </header>
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
