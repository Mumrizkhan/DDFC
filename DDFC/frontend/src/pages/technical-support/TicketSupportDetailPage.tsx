import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Send, CheckCircle, RotateCcw, User } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchTicketById,
  addTicketReply,
  resolveTicket,
  reopenTicket,
  assignTicketToUser,
  clearCurrentTicket,
} from '../../store/slices/ticketsSlice';
import { Card } from '../../components/ui/Card';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';
import type { TicketReply } from '../../types';

const statusColors: Record<string, string> = {
  Open:       'bg-blue-100 text-blue-700',
  InProgress: 'bg-amber-100 text-amber-700',
  Resolved:   'bg-emerald-100 text-emerald-700',
  Closed:     'bg-gray-100 text-gray-600',
};

export const TicketSupportDetailPage: React.FC = () => {
  const { ticketId } = useParams<{ ticketId: string }>();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const { currentTicket, loading, actionLoading } = useAppSelector((s) => s.tickets);
  const staffUsers = useAppSelector((s) => s.auth.staffUser);

  const [replyText, setReplyText]           = useState('');
  const [assignUserId, setAssignUserId]     = useState('');
  const [showAssign, setShowAssign]         = useState(false);

  useEffect(() => {
    if (ticketId) dispatch(fetchTicketById(ticketId));
    return () => { dispatch(clearCurrentTicket()); };
  }, [dispatch, ticketId]);

  if (loading || !currentTicket) return <PageLoader />;

  const handleReply = async () => {
    if (!replyText.trim() || !ticketId) return;
    await dispatch(addTicketReply({ ticketId, messageBody: replyText.trim() }));
    setReplyText('');
    // Refresh to get latest replies
    dispatch(fetchTicketById(ticketId));
  };

  const handleResolve = async () => {
    if (!ticketId) return;
    await dispatch(resolveTicket(ticketId));
    dispatch(fetchTicketById(ticketId));
  };

  const handleReopen = async () => {
    if (!ticketId) return;
    await dispatch(reopenTicket(ticketId));
    dispatch(fetchTicketById(ticketId));
  };

  const handleAssign = async () => {
    if (!ticketId || !assignUserId.trim()) return;
    await dispatch(assignTicketToUser({ ticketId, userId: assignUserId.trim() }));
    setAssignUserId('');
    setShowAssign(false);
    dispatch(fetchTicketById(ticketId));
  };

  const isClosed   = currentTicket.status === 'Closed';
  const isResolved = currentTicket.status === 'Resolved';

  return (
    <div className="space-y-5 max-w-3xl">
      {/* Back */}
      <button
        onClick={() => navigate('/backoffice/tickets')}
        className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700"
      >
        <ArrowLeft size={16} />
        Back to Queue
      </button>

      {/* Header */}
      <Card>
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div className="min-w-0">
            <h1 className="text-lg font-bold text-gray-900 break-words">{currentTicket.subject}</h1>
            <p className="text-sm text-gray-500 mt-1">
              {currentTicket.category}
              {currentTicket.customerName ? ` · ${currentTicket.customerName}` : ''}
              {' · '}
              {format(new Date(currentTicket.createdAt), 'dd MMM yyyy, HH:mm')}
            </p>
            {currentTicket.assignedUserName && (
              <p className="text-xs text-gray-400 mt-0.5 flex items-center gap-1">
                <User size={12} />
                Assigned to {currentTicket.assignedUserName}
              </p>
            )}
          </div>
          <span className={`text-xs font-semibold px-3 py-1 rounded-full ${statusColors[currentTicket.status] ?? 'bg-gray-100'}`}>
            {currentTicket.status}
          </span>
        </div>

        <p className="mt-4 text-sm text-gray-700 border-t pt-4">{currentTicket.description}</p>

        {/* Action buttons */}
        <div className="mt-4 flex flex-wrap gap-2 border-t pt-4">
          {!isClosed && !isResolved && (
            <button
              disabled={actionLoading}
              onClick={handleResolve}
              className="flex items-center gap-1.5 px-4 py-2 bg-emerald-600 text-white text-sm rounded-lg hover:bg-emerald-700 disabled:opacity-50 transition-colors"
            >
              <CheckCircle size={15} />
              Mark as Resolved
            </button>
          )}
          {(isResolved || isClosed) && (
            <button
              disabled={actionLoading}
              onClick={handleReopen}
              className="flex items-center gap-1.5 px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              <RotateCcw size={15} />
              Reopen Ticket
            </button>
          )}
          {!isClosed && (
            <button
              onClick={() => setShowAssign(!showAssign)}
              className="flex items-center gap-1.5 px-4 py-2 border border-gray-300 text-gray-700 text-sm rounded-lg hover:bg-gray-50 transition-colors"
            >
              <User size={15} />
              Assign to Agent
            </button>
          )}
        </div>

        {/* Assign panel */}
        {showAssign && (
          <div className="mt-3 flex gap-2">
            <input
              type="text"
              placeholder="Paste agent User ID…"
              value={assignUserId}
              onChange={(e) => setAssignUserId(e.target.value)}
              className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-emerald-500 outline-none"
            />
            <button
              disabled={actionLoading || !assignUserId.trim()}
              onClick={handleAssign}
              className="px-4 py-2 bg-slate-700 text-white text-sm rounded-lg hover:bg-slate-800 disabled:opacity-50 transition-colors"
            >
              Assign
            </button>
          </div>
        )}
      </Card>

      {/* Conversation */}
      <div>
        <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
          Conversation ({currentTicket.replies?.length ?? 0} messages)
        </h2>
        <div className="space-y-3">
          {(currentTicket.replies ?? []).map((reply: TicketReply) => {
            const isStaff = reply.authorType === 'Staff';
            return (
              <div
                key={reply.replyId}
                className={`flex ${isStaff ? 'justify-end' : 'justify-start'}`}
              >
                <div
                  className={`max-w-[80%] rounded-xl px-4 py-2.5 text-sm ${
                    isStaff
                      ? 'bg-emerald-600 text-white'
                      : 'bg-white border border-gray-200 text-gray-800'
                  }`}
                >
                  <p className={`text-xs font-medium mb-1 ${isStaff ? 'text-emerald-100' : 'text-gray-400'}`}>
                    {isStaff ? (reply.authorName ?? 'Support Agent') : (reply.authorName ?? 'Customer')}
                  </p>
                  <p className="leading-relaxed">{reply.messageBody}</p>
                  <p className={`text-xs mt-1 ${isStaff ? 'text-emerald-200' : 'text-gray-400'}`}>
                    {format(new Date(reply.createdAt), 'dd MMM, HH:mm')}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Reply box */}
      {!isClosed && (
        <Card>
          <textarea
            rows={3}
            placeholder="Type your reply…"
            value={replyText}
            onChange={(e) => setReplyText(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm resize-none focus:ring-2 focus:ring-emerald-500 outline-none"
          />
          <div className="flex justify-end mt-2">
            <button
              disabled={actionLoading || !replyText.trim()}
              onClick={handleReply}
              className="flex items-center gap-2 px-4 py-2 bg-emerald-600 text-white text-sm rounded-lg hover:bg-emerald-700 disabled:opacity-50 transition-colors"
            >
              <Send size={14} />
              Send Reply
            </button>
          </div>
        </Card>
      )}
    </div>
  );
};
