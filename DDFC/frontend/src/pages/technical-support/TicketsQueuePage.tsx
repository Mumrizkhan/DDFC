import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Filter } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchQueueTickets } from '../../store/slices/ticketsSlice';
import { Card } from '../../components/ui/Card';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';
import type { TicketStatus, TicketCategory } from '../../types';

const STATUS_OPTIONS: Array<TicketStatus | 'All'> = ['All', 'Open', 'InProgress', 'Resolved', 'Closed'];
const statusColors: Record<string, string> = {
  Open:       'bg-blue-100 text-blue-700',
  InProgress: 'bg-amber-100 text-amber-700',
  Resolved:   'bg-emerald-100 text-emerald-700',
  Closed:     'bg-gray-100 text-gray-600',
};

export const TicketsQueuePage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { tickets, loading } = useAppSelector((s) => s.tickets);

  const [search, setSearch]     = useState('');
  const [statusFilter, setStatusFilter] = useState<TicketStatus | 'All'>('All');
  const [categoryFilter, setCategoryFilter] = useState<TicketCategory | 'All'>('All');

  useEffect(() => {
    dispatch(fetchQueueTickets());
  }, [dispatch]);

  if (loading) return <PageLoader />;

  const categories = Array.from(new Set(tickets.map((t) => t.category))) as TicketCategory[];

  const filtered = tickets.filter((t) => {
    const matchesSearch =
      search.trim() === '' ||
      t.subject.toLowerCase().includes(search.toLowerCase()) ||
      (t.customerName ?? '').toLowerCase().includes(search.toLowerCase());
    const matchesStatus   = statusFilter   === 'All' || t.status   === statusFilter;
    const matchesCategory = categoryFilter === 'All' || t.category === categoryFilter;
    return matchesSearch && matchesStatus && matchesCategory;
  });

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-bold text-gray-900">Tickets Queue</h1>

      {/* Filters */}
      <Card>
        <div className="flex flex-wrap gap-3">
          <div className="relative flex-1 min-w-48">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Search subject or customer…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-emerald-500 focus:border-transparent outline-none"
            />
          </div>

          <div className="flex items-center gap-2">
            <Filter size={16} className="text-gray-400" />
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as TicketStatus | 'All')}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-emerald-500 outline-none"
            >
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>{s === 'All' ? 'All Statuses' : s}</option>
              ))}
            </select>

            <select
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value as TicketCategory | 'All')}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-emerald-500 outline-none"
            >
              <option value="All">All Categories</option>
              {categories.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>
        </div>
      </Card>

      {/* Results */}
      <p className="text-sm text-gray-500">{filtered.length} ticket{filtered.length !== 1 ? 's' : ''}</p>

      {filtered.length === 0 ? (
        <Card>
          <p className="text-center text-gray-400 py-8">No tickets match your filters</p>
        </Card>
      ) : (
        <div className="space-y-2">
          {filtered.map((ticket) => (
            <div
              key={ticket.ticketId}
              className="bg-white border border-gray-200 rounded-lg px-4 py-3 flex items-center justify-between cursor-pointer hover:shadow-sm transition-shadow"
              onClick={() => navigate(`/backoffice/tickets/${ticket.ticketId}`)}
            >
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <p className="font-medium text-gray-900 text-sm truncate">{ticket.subject}</p>
                  <span className="text-xs text-gray-400">{ticket.category}</span>
                </div>
                <p className="text-xs text-gray-500 mt-0.5">
                  {ticket.customerName ?? 'Customer'}
                  {ticket.assignedUserName ? ` · Assigned to ${ticket.assignedUserName}` : ''}
                  {' · '}
                  {format(new Date(ticket.createdAt), 'dd MMM yyyy')}
                </p>
              </div>
              <span className={`ml-4 text-xs font-medium px-2 py-0.5 rounded-full shrink-0 ${statusColors[ticket.status] ?? 'bg-gray-100 text-gray-600'}`}>
                {ticket.status}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
