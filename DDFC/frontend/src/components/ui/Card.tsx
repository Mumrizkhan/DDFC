import React from 'react';

interface CardProps {
  children: React.ReactNode;
  className?: string;
  title?: string;
  subtitle?: string;
  action?: React.ReactNode;
}

export const Card: React.FC<CardProps> = ({
  children,
  className = '',
  title,
  subtitle,
  action,
}) => (
  <div className={`bg-white rounded-xl shadow-sm border border-gray-200 ${className}`}>
    {(title || action) && (
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
        <div>
          {title && <h3 className="text-base font-semibold text-gray-900">{title}</h3>}
          {subtitle && <p className="text-sm text-gray-500 mt-0.5">{subtitle}</p>}
        </div>
        {action && <div>{action}</div>}
      </div>
    )}
    <div className="p-6">{children}</div>
  </div>
);

interface StatCardProps {
  title: string;
  value: string | number;
  icon?: React.ReactNode;
  change?: string;
  changeType?: 'positive' | 'negative' | 'neutral';
  color?: 'blue' | 'green' | 'yellow' | 'red' | 'purple';
}

const colorMap: Record<string, { bg: string; icon: string; text: string }> = {
  blue: { bg: 'bg-blue-50', icon: 'bg-blue-500', text: 'text-blue-600' },
  green: { bg: 'bg-green-50', icon: 'bg-green-500', text: 'text-green-600' },
  yellow: { bg: 'bg-yellow-50', icon: 'bg-yellow-500', text: 'text-yellow-600' },
  red: { bg: 'bg-red-50', icon: 'bg-red-500', text: 'text-red-600' },
  purple: { bg: 'bg-purple-50', icon: 'bg-purple-500', text: 'text-purple-600' },
};

export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  icon,
  change,
  changeType = 'neutral',
  color = 'blue',
}) => {
  const colors = colorMap[color];
  return (
    <div className={`rounded-xl p-5 ${colors.bg} border border-opacity-20`}>
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-600">{title}</p>
          <p className={`text-2xl font-bold mt-1 ${colors.text}`}>{value}</p>
          {change && (
            <p
              className={`text-xs mt-1 ${
                changeType === 'positive'
                  ? 'text-green-600'
                  : changeType === 'negative'
                  ? 'text-red-600'
                  : 'text-gray-500'
              }`}
            >
              {change}
            </p>
          )}
        </div>
        {icon && (
          <div className={`w-12 h-12 rounded-xl ${colors.icon} flex items-center justify-center text-white`}>
            {icon}
          </div>
        )}
      </div>
    </div>
  );
};
