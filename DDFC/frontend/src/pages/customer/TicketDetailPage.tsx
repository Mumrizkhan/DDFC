import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Send } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchTicketById, addTicketReply } from '../../store/slices/ticketsSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Textarea } from '../../components/ui/Input';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';
import { toast } from 'react-toastify';

export const TicketDetailPage: React.FC = () => {
  const { ticketId } = useParams<{ ticketId: string }>();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { currentTicket, loading } = useAppSelector((s) => s.tickets);
  const { customer } = useAppSelector((s) => s.auth);
  const [reply, setReply] = useState('');
  const [sending, setSending] = useState(false);

  useEffect(() => {
    if (ticketId) dispatch(fetchTicketById(ticketId));
  }, [dispatch, ticketId]);

  const handleReply = async () => {
    if (!reply.trim() || !ticketId) return;
    setSending(true);
    try {
      await dispatch(addTicketReply({ ticketId, messageBody: reply })).unwrap();
      setReply('');
      toast.success('Reply sent');
    } catch {
      toast.error('Failed to send reply');
    } finally {
      setSending(false);
    }
  };

  if (loading || !currentTicket) return <PageLoader />;

  const t = currentTicket;

  return (
    <div className="max-w-3xl mx-auto p-4 space-y-5">
      <div className="flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="text-gray-400 hover:text-gray-700">
          <ArrowLeft size={20} />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h1 className="text-lg font-bold text-gray-900">{t.subject}</h1>
            <Badge
              variant={
                t.status === 'Open' ? 'warning' : t.status === 'Resolved' ? 'success' : 'default'
              }
              size="sm"
            >
              {t.status}
            </Badge>
          </div>
          <p className="text-xs text-gray-400">
            {t.category} • Opened {format(new Date(t.createdAt), 'dd MMM yyyy')}
          </p>
        </div>
      </div>

      {/* Thread */}
      <Card>
        <div className="space-y-4">
          {/* Original message */}
          <div className="flex gap-3">
            <div className="w-8 h-8 rounded-full bg-dha-green text-white flex items-center justify-center text-sm font-bold shrink-0">
              {(customer?.fullName ?? 'C')[0]}
            </div>
            <div className="flex-1">
              <div className="bg-gray-50 rounded-lg p-3">
                <p className="text-sm text-gray-800">{t.description}</p>
              </div>
              <p className="text-xs text-gray-400 mt-1">
                {format(new Date(t.createdAt), 'dd MMM yyyy, HH:mm')}
              </p>
            </div>
          </div>

          {/* Replies */}
          {(t.replies ?? []).map((r) => (
            <div
              key={r.replyId}
              className={`flex gap-3 ${r.authorType === 'Staff' ? 'flex-row-reverse' : ''}`}
            >
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold shrink-0 ${
                  r.authorType === 'Staff' ? 'bg-blue-600 text-white' : 'bg-dha-green text-white'
                }`}
              >
                {r.authorType === 'Staff' ? 'S' : (customer?.fullName ?? 'C')[0]}
              </div>
              <div className="flex-1">
                <div
                  className={`rounded-lg p-3 ${
                    r.authorType === 'Staff' ? 'bg-blue-50 text-right' : 'bg-gray-50'
                  }`}
                >
                  <p className="text-sm text-gray-800">{r.messageBody}</p>
                </div>
                <p
                  className={`text-xs text-gray-400 mt-1 ${r.authorType === 'Staff' ? 'text-right' : ''}`}
                >
                  {format(new Date(r.createdAt), 'dd MMM yyyy, HH:mm')}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Reply Box */}
      {t.status !== 'Resolved' && (
        <Card title="Reply">
          <Textarea
            placeholder="Type your message..."
            value={reply}
            onChange={(e) => setReply(e.target.value)}
            rows={4}
          />
          <Button
            variant="primary"
            className="mt-3"
            loading={sending}
            disabled={!reply.trim()}
            onClick={handleReply}
            icon={<Send size={14} />}
          >
            Send Reply
          </Button>
        </Card>
      )}
    </div>
  );
};
