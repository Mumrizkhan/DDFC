import React, { useEffect, useState } from 'react';
import { Bell } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';

export const StaffNotificationBell: React.FC<{ path: string }> = ({ path }) => {
  const navigate = useNavigate();
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    let active = true;
    const refresh = () => {
      api.get<{ isRead: boolean }[]>('/staff-notifications')
        .then((response) => { if (active) setUnread(response.data.filter((item) => !item.isRead).length); })
        .catch(() => {});
    };
    refresh();
    const timer = window.setInterval(refresh, 30000);
    window.addEventListener('staff-notifications-changed', refresh);
    return () => {
      active = false;
      window.clearInterval(timer);
      window.removeEventListener('staff-notifications-changed', refresh);
    };
  }, []);

  return (
    <button type="button" title="Notifications" aria-label={`Notifications${unread ? `, ${unread} unread` : ''}`}
      onClick={() => navigate(path)} className="relative text-gray-500 hover:text-gray-700 p-1">
      <Bell size={20} />
      {unread > 0 && <span className="absolute -top-1 -right-1 min-w-4 h-4 rounded-full bg-red-600 text-white text-[10px] leading-4 text-center">{unread > 99 ? '99+' : unread}</span>}
    </button>
  );
};