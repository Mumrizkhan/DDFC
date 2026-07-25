import React, { useEffect, useState } from 'react';
import { CalendarDays, Plus, CheckCircle, XCircle, UserX, Phone, Building2, Clock, Users } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchAppointments, completeAppointment, cancelAppointment, noShowAppointment } from '../../store/slices/appointmentsSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Badge } from '../../components/ui/Badge';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { Input, Select } from '../../components/ui/Input';
import { BookAppointmentModal } from '../../components/appointments/BookAppointmentModal';
import { toast } from 'react-toastify';
import type { AppointmentStatus } from '../../types';
import { format } from 'date-fns';

const STATUS_VARIANT: Record<AppointmentStatus, 'info' | 'success' | 'danger' | 'secondary'> = {
  Scheduled:  'info',
  Completed:  'success',
  Cancelled:  'danger',
  NoShow:     'secondary',
};

export const AppointmentsPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { appointments, loading } = useAppSelector((s) => s.appointments);

  const [showBook,     setShowBook]     = useState(false);
  const [dateFilter,   setDateFilter]   = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [actioning,    setActioning]    = useState<string | null>(null);

  useEffect(() => {
    dispatch(fetchAppointments({
      date:   dateFilter   || undefined,
      status: statusFilter || undefined,
    }));
  }, [dispatch, dateFilter, statusFilter]);

  const handleAction = async (
    id: string,
    action: 'complete' | 'cancel' | 'noShow',
    notes?: string
  ) => {
    setActioning(id);
    try {
      if (action === 'complete') await dispatch(completeAppointment({ id, notes })).unwrap();
      if (action === 'cancel')   await dispatch(cancelAppointment({ id, notes })).unwrap();
      if (action === 'noShow')   await dispatch(noShowAppointment(id)).unwrap();
      toast.success('Appointment updated');
    } catch {
      toast.error('Action failed');
    } finally {
      setActioning(null);
    }
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <CalendarDays size={22} className="text-blue-600" />
            Appointments
          </h1>
          <p className="text-gray-500 text-sm mt-0.5">
            Client appointments across all departments
          </p>
        </div>
        <Button
          variant="primary"
          icon={<Plus size={16} />}
          onClick={() => setShowBook(true)}
        >
          Book Appointment
        </Button>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <Input
          type="date"
          value={dateFilter}
          onChange={(e) => setDateFilter(e.target.value)}
          className="w-44"
          placeholder="Filter by date"
        />
        <Select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          options={[
            { value: '',          label: 'All Statuses' },
            { value: 'Scheduled', label: 'Scheduled' },
            { value: 'Completed', label: 'Completed' },
            { value: 'Cancelled', label: 'Cancelled' },
            { value: 'NoShow',    label: 'No Show' },
          ]}
          className="w-40"
        />
      </div>

      {/* List */}
      {appointments.length === 0 ? (
        <Card>
          <div className="py-12 text-center text-gray-400">
            <CalendarDays size={36} className="mx-auto mb-3 opacity-30" />
            <p>No appointments found.</p>
          </div>
        </Card>
      ) : (
        <div className="space-y-3">
          {appointments.map((appt) => (
            <Card key={appt.id}>
              <div className="flex items-start justify-between gap-4 flex-wrap">
                {/* Left: core info */}
                <div className="space-y-1.5 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-semibold text-gray-900">{appt.customerName ?? appt.customerId}</span>
                    <Badge variant={STATUS_VARIANT[appt.status]}>{appt.status}</Badge>
                    <span className="text-xs text-gray-400">
                      {appt.bookingMethod === 'PhoneCall'
                        ? <><Phone size={11} className="inline mr-0.5" />Phone</>
                        : <><Building2 size={11} className="inline mr-0.5" />In-Person</>}
                    </span>
                  </div>

                  <div className="flex items-center gap-3 text-sm text-gray-600 flex-wrap">
                    <span className="flex items-center gap-1">
                      <CalendarDays size={13} />
                      {appt.appointmentDate} &nbsp;
                      <Clock size={13} />
                      {appt.startTime} ({appt.durationMinutes} min)
                    </span>
                    <span className="text-gray-400">•</span>
                    <span>{appt.departmentName ?? 'Department'}</span>
                    {appt.assignedEmployeeName && (
                      <><span className="text-gray-400">•</span><span>👤 {appt.assignedEmployeeName}</span></>
                    )}
                  </div>

                  <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap">
                    <span className="flex items-center gap-1">
                      <Users size={12} />
                      {appt.numberOfPersons} person{appt.numberOfPersons > 1 ? 's' : ''}
                      {appt.attendeeNames ? `: ${appt.attendeeNames}` : ''}
                    </span>
                    {appt.purpose && <span className="text-blue-600">📋 {appt.purpose}</span>}
                    {appt.requestCode && <span className="text-gray-400">Req: {appt.requestCode}</span>}
                  </div>

                  {appt.notes && (
                    <p className="text-xs text-gray-400 italic">{appt.notes}</p>
                  )}
                  {appt.cancellationReason && (
                    <p className="text-xs text-red-500">Cancelled: {appt.cancellationReason}</p>
                  )}
                </div>

                {/* Right: actions (only for Scheduled) */}
                {appt.status === 'Scheduled' && (
                  <div className="flex gap-2 shrink-0">
                    <Button
                      variant="primary"
                      size="sm"
                      loading={actioning === appt.id}
                      icon={<CheckCircle size={13} />}
                      onClick={() => handleAction(appt.id, 'complete')}
                    >
                      Complete
                    </Button>
                    <Button
                      variant="secondary"
                      size="sm"
                      loading={actioning === appt.id}
                      icon={<UserX size={13} />}
                      onClick={() => handleAction(appt.id, 'noShow')}
                    >
                      No-show
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      loading={actioning === appt.id}
                      icon={<XCircle size={13} />}
                      onClick={() => {
                        const reason = window.prompt('Cancellation reason (optional):') ?? undefined;
                        handleAction(appt.id, 'cancel', reason);
                      }}
                    >
                      Cancel
                    </Button>
                  </div>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}

      <BookAppointmentModal
        isOpen={showBook}
        onClose={() => setShowBook(false)}
        onBooked={() => dispatch(fetchAppointments({
          date:   dateFilter   || undefined,
          status: statusFilter || undefined,
        }))}
      />
    </div>
  );
};
