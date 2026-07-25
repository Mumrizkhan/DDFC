import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, MessageCircle, ChevronRight } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchMyTickets, createTicket } from '../../store/slices/ticketsSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { Input, Select, Textarea } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { format } from 'date-fns';
import { toast } from 'react-toastify';

const CATEGORY_OPTIONS = [
  { value: 'General', label: 'General Inquiry' },
  { value: 'Technical', label: 'Technical Issue' },
  { value: 'Payment', label: 'Payment Issue' },
  { value: 'Document', label: 'Document Request' },
  { value: 'Complaint', label: 'Complaint' },
];

export const TicketsPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { tickets, loading } = useAppSelector((s) => s.tickets);
  const [showModal, setShowModal] = useState(false);
  const [subject, setSubject] = useState('');
  const [category, setCategory] = useState('General');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    dispatch(fetchMyTickets());
  }, [dispatch]);

  const handleCreate = async () => {
    if (!subject.trim() || !description.trim()) return;
    setSubmitting(true);
    try {
      await dispatch(createTicket({ subject, category, description })).unwrap();
      toast.success('Ticket submitted');
      setShowModal(false);
      setSubject('');
      setDescription('');
      setCategory('General');
    } catch {
      toast.error('Failed to submit ticket');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <PageLoader />;

  return (
    <div className="max-w-3xl mx-auto p-4 space-y-5">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Support Tickets</h1>
        <Button variant="primary" onClick={() => setShowModal(true)} icon={<Plus size={16} />}>
          New Ticket
        </Button>
      </div>

      <Card>
        {tickets.length === 0 ? (
          <div className="text-center py-12 text-gray-400">
            <MessageCircle size={36} className="mx-auto mb-2 opacity-30" />
            <p>No support tickets</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-50">
            {tickets.map((t) => (
              <div
                key={t.ticketId}
                className="py-3 flex items-center justify-between cursor-pointer hover:bg-gray-50 transition-colors"
                onClick={() => navigate(`/portal/tickets/${t.ticketId}`)}
              >
                <div>
                  <p className="text-sm font-medium text-gray-900">{t.subject}</p>
                  <p className="text-xs text-gray-400">
                    {t.category} • {format(new Date(t.createdAt), 'dd MMM yyyy')}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge
                    variant={
                      t.status === 'Open'
                        ? 'warning'
                        : t.status === 'Resolved'
                        ? 'success'
                        : 'default'
                    }
                    size="sm"
                  >
                    {t.status}
                  </Badge>
                  <ChevronRight size={16} className="text-gray-400" />
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title="New Support Ticket"
        footer={
          <div className="flex gap-2 justify-end">
            <Button variant="ghost" onClick={() => setShowModal(false)}>Cancel</Button>
            <Button variant="primary" loading={submitting} onClick={handleCreate}>
              Submit
            </Button>
          </div>
        }
      >
        <div className="space-y-4">
          <Input
            label="Subject"
            placeholder="Brief description of your issue"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
          />
          <Select
            label="Category"
            options={CATEGORY_OPTIONS}
            value={category}
            onChange={(e) => setCategory(e.target.value)}
          />
          <Textarea
            label="Description"
            placeholder="Provide detailed information..."
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={5}
          />
        </div>
      </Modal>
    </div>
  );
};
