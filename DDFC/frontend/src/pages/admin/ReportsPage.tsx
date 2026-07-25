import React from 'react';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { BarChart3, Download } from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import api from '../../services/api';
import { useState, useEffect } from 'react';

export const ReportsPage: React.FC = () => {
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [dailyData, setDailyData] = useState<{ date: string; new: number; completed: number; rejected: number }[]>([]);

  useEffect(() => {
    api
      .get('/reports/daily')
      .then((r) => setDailyData(r.data))
      .catch(() => {});
  }, []);

  const handleExport = (type: 'pdf' | 'excel') => {
    api
      .get(`/reports/daily?format=${type}&from=${dateFrom}&to=${dateTo}`, {
        responseType: 'blob',
      })
      .then((r) => {
        const url = window.URL.createObjectURL(r.data);
        const a = document.createElement('a');
        a.href = url;
        a.download = `ddfc-report.${type === 'pdf' ? 'pdf' : 'xlsx'}`;
        a.click();
      })
      .catch(() => {});
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Reports & Analytics</h1>
          <p className="text-gray-500 text-sm mt-1">
            System-wide reporting and performance insights.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <input
            type="date"
            value={dateFrom}
            onChange={(e) => setDateFrom(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
          <span className="text-gray-400 text-sm">to</span>
          <input
            type="date"
            value={dateTo}
            onChange={(e) => setDateTo(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('pdf')}
          >
            <Download size={14} /> PDF
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('excel')}
          >
            <Download size={14} /> Excel
          </Button>
        </div>
      </div>

      <Card title="Daily Request Summary" subtitle="New, completed, and rejected requests">
        {dailyData.length > 0 ? (
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={dailyData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip />
              <Bar dataKey="new" fill="#3b82f6" name="New" radius={[4, 4, 0, 0]} />
              <Bar dataKey="completed" fill="#10b981" name="Completed" radius={[4, 4, 0, 0]} />
              <Bar dataKey="rejected" fill="#ef4444" name="Rejected" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        ) : (
          <div className="flex flex-col items-center justify-center py-16 text-gray-400">
            <BarChart3 size={48} className="mb-3 opacity-50" />
            <p>No report data available yet</p>
          </div>
        )}
      </Card>
    </div>
  );
};
