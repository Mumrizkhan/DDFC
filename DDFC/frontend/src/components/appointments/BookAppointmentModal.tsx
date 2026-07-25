import React, { useEffect, useMemo, useState } from 'react';
import { X, UserCheck, Search } from 'lucide-react';
import { Modal } from '../../components/ui/Modal';
import { Button } from '../../components/ui/Button';
import { Input, Select, Textarea } from '../../components/ui/Input';
import { useAppDispatch } from '../../store/hooks';
import { bookAppointment } from '../../store/slices/appointmentsSlice';
import { adminService, appointmentsService } from '../../services/endpoints';
import { toast } from 'react-toastify';
import type { Appointment, Customer, Department, PossessionRequest, User } from '../../types';
import api from '../../services/api';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  /** Pre-fill from a request context */
  customerId?: string;
  requestId?: string;
  onBooked?: (appt: Appointment) => void;
}

export const BookAppointmentModal: React.FC<Props> = ({
  isOpen, onClose, customerId: initCustomerId, requestId: initRequestId, onBooked,
}) => {
  const dispatch = useAppDispatch();

  // ── Customer search ────────────────────────────────────────────────────────
  const [customers,         setCustomers]         = useState<Customer[]>([]);
  const [loadingCustomers,  setLoadingCustomers]  = useState(false);
  const [searchQuery,       setSearchQuery]       = useState('');
  const [selectedCustomer,  setSelectedCustomer]  = useState<Customer | null>(null);

  // ── Customer's requests ───────────────────────────────────────────────────
  const [customerRequests,  setCustomerRequests]  = useState<PossessionRequest[]>([]);
  const [loadingRequests,   setLoadingRequests]   = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState(initRequestId ?? '');

  // ── Department + employee ─────────────────────────────────────────────────
  const [departments,       setDepartments]       = useState<Department[]>([]);
  const [allDeptUsers,      setAllDeptUsers]      = useState<User[]>([]);
  const [availableEmpIds,   setAvailableEmpIds]   = useState<Set<string>>(new Set());
  const [loadingUsers,      setLoadingUsers]      = useState(false);
  const [departmentId,      setDepartmentId]      = useState('');
  const [assignedEmpId,     setAssignedEmpId]     = useState('');

  // ── Schedule ──────────────────────────────────────────────────────────────
  const [appointmentDate,   setAppointmentDate]   = useState('');
  const [startTime,         setStartTime]         = useState('09:00');
  const [duration,          setDuration]          = useState(30);

  // ── Booking details ───────────────────────────────────────────────────────
  const [numberOfPersons,   setNumberOfPersons]   = useState(1);
  const [attendeeNames,     setAttendeeNames]      = useState('');
  const [bookingMethod,     setBookingMethod]      = useState<'PhoneCall' | 'InPerson'>('InPerson');
  const [purpose,           setPurpose]           = useState('');
  const [notes,             setNotes]             = useState('');
  const [submitting,        setSubmitting]        = useState(false);

  // ── Load customers + departments on open ──────────────────────────────────
  useEffect(() => {
    if (!isOpen) return;
    setLoadingCustomers(true);
    adminService.getCustomers()
      .then(setCustomers)
      .catch(() => toast.error('Failed to load customers'))
      .finally(() => setLoadingCustomers(false));

    adminService.getDepartments().then(setDepartments).catch(() => {});
  }, [isOpen]);

  // If pre-filled with customerId, find and select that customer
  useEffect(() => {
    if (!isOpen || !initCustomerId || customers.length === 0) return;
    const c = customers.find((x) => x.customerId === initCustomerId);
    if (c) setSelectedCustomer(c);
  }, [isOpen, initCustomerId, customers]);

  // ── Customer search filter ─────────────────────────────────────────────────
  const filteredCustomers = useMemo(() => {
    if (!searchQuery.trim() || selectedCustomer) return [];
    const q = searchQuery.toLowerCase().trim();
    return customers
      .filter((c) =>
        c.fullName.toLowerCase().includes(q) ||
        c.cnic.toLowerCase().includes(q) ||
        c.phoneNumber.includes(q)
      )
      .slice(0, 8);
  }, [searchQuery, customers, selectedCustomer]);

  // ── Load customer's requests when customer is selected ────────────────────
  useEffect(() => {
    if (!selectedCustomer) { setCustomerRequests([]); return; }
    if (initRequestId) { setSelectedRequestId(initRequestId); return; }
    setLoadingRequests(true);
    api.get<PossessionRequest[]>('/requests')
      .then((r) => {
        const mine = r.data.filter((req) => req.customerId === selectedCustomer.customerId);
        setCustomerRequests(mine);
      })
      .catch(() => {})
      .finally(() => setLoadingRequests(false));
  }, [selectedCustomer, initRequestId]);

  // ── Load dept employees when department changes ────────────────────────────
  useEffect(() => {
    if (!departmentId) { setAllDeptUsers([]); setAvailableEmpIds(new Set()); return; }
    setLoadingUsers(true);
    setAssignedEmpId('');
    adminService.getUsers()
      .then((users) => setAllDeptUsers(users.filter((u) => u.departmentId === departmentId && u.isActive)))
      .catch(() => {})
      .finally(() => setLoadingUsers(false));
  }, [departmentId]);

  // ── Check availability when date + time + duration change ─────────────────
  useEffect(() => {
    if (!departmentId || !appointmentDate || !startTime) { setAvailableEmpIds(new Set()); return; }
    appointmentsService
      .getAvailability(departmentId, appointmentDate, startTime, duration)
      .then((emps) => setAvailableEmpIds(new Set(emps.map((e) => e.id))))
      .catch(() => setAvailableEmpIds(new Set()));
  }, [departmentId, appointmentDate, startTime, duration]);

  // ── Submit ────────────────────────────────────────────────────────────────
  const handleSubmit = async () => {
    if (!selectedCustomer) { toast.error('Please select a customer'); return; }
    if (!departmentId)     { toast.error('Please select a department'); return; }
    if (!appointmentDate)  { toast.error('Please select a date'); return; }

    setSubmitting(true);
    try {
      const appt = await dispatch(bookAppointment({
        customerId:         selectedCustomer.customerId,
        requestId:          selectedRequestId || undefined,
        departmentId,
        assignedEmployeeId: assignedEmpId || undefined,
        appointmentDate,
        startTime,
        durationMinutes:    duration,
        numberOfPersons,
        attendeeNames:      attendeeNames.trim() || undefined,
        bookingMethod,
        purpose:            purpose.trim() || undefined,
        notes:              notes.trim() || undefined,
      })).unwrap();
      toast.success('Appointment booked successfully');
      onBooked?.(appt);
      handleClose();
    } catch (err) {
      toast.error((err as string) || 'Failed to book appointment');
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setSearchQuery('');
    setSelectedCustomer(null);
    setCustomerRequests([]);
    setSelectedRequestId(initRequestId ?? '');
    setDepartmentId('');
    setAllDeptUsers([]);
    setAvailableEmpIds(new Set());
    setAssignedEmpId('');
    setAppointmentDate('');
    setStartTime('09:00');
    setDuration(30);
    setNumberOfPersons(1);
    setAttendeeNames('');
    setBookingMethod('InPerson');
    setPurpose('');
    setNotes('');
    onClose();
  };

  const deptName = departments.find((d) => d.departmentId === departmentId)?.departmentName ?? '';

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Book Appointment"
      size="xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={handleClose}>Cancel</Button>
          <Button
            variant="primary"
            loading={submitting}
            disabled={!selectedCustomer || !departmentId || !appointmentDate}
            onClick={handleSubmit}
          >
            Book Appointment
          </Button>
        </div>
      }
    >
      <div className="space-y-5">

        {/* ── Booking method ── */}
        <div>
          <p className="text-sm font-medium text-gray-700 mb-2">Booking Method</p>
          <div className="flex gap-3">
            {(['InPerson', 'PhoneCall'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => setBookingMethod(m)}
                className={`flex-1 py-2 rounded-lg border-2 text-sm font-medium transition-colors
                  ${bookingMethod === m
                    ? 'border-blue-500 bg-blue-50 text-blue-700'
                    : 'border-gray-200 text-gray-600 hover:bg-gray-50'}`}
              >
                {m === 'InPerson' ? '🏢 In Person' : '📞 Phone Call'}
              </button>
            ))}
          </div>
        </div>

        {/* ── Customer search ── */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Customer <span className="text-red-500">*</span>
          </label>

          {selectedCustomer ? (
            <div className="flex items-start justify-between gap-2 p-3 bg-green-50 border border-green-200 rounded-lg">
              <div className="space-y-0.5">
                <p className="text-sm font-semibold text-green-800 flex items-center gap-1">
                  <UserCheck size={14} /> {selectedCustomer.fullName}
                </p>
                <p className="text-xs text-green-700">CNIC: {selectedCustomer.cnic}</p>
                <p className="text-xs text-green-700">Mobile: {selectedCustomer.phoneNumber}</p>
              </div>
              {!initCustomerId && (
                <button
                  type="button"
                  onClick={() => { setSelectedCustomer(null); setCustomerRequests([]); setSelectedRequestId(''); }}
                  className="p-1 rounded hover:bg-green-200 text-green-700 flex-shrink-0"
                  title="Change customer"
                >
                  <X size={14} />
                </button>
              )}
            </div>
          ) : (
            <div className="relative">
              <div className="relative">
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder={loadingCustomers ? 'Loading customers…' : 'Search by name, CNIC or mobile number'}
                  disabled={loadingCustomers}
                  className="w-full pl-9 pr-3 py-2 rounded-lg border border-gray-300 text-sm
                    placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500
                    disabled:bg-gray-50 disabled:text-gray-500"
                />
              </div>
              {filteredCustomers.length > 0 && (
                <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-20 max-h-48 overflow-y-auto">
                  {filteredCustomers.map((c) => (
                    <button
                      key={c.customerId}
                      type="button"
                      onClick={() => { setSelectedCustomer(c); setSearchQuery(''); }}
                      className="w-full text-left px-4 py-2.5 hover:bg-blue-50 border-b last:border-b-0 transition-colors"
                    >
                      <p className="font-medium text-gray-900 text-sm">{c.fullName}</p>
                      <p className="text-xs text-gray-500">CNIC: {c.cnic} &nbsp;|&nbsp; Mobile: {c.phoneNumber}</p>
                    </button>
                  ))}
                </div>
              )}
              {searchQuery.trim() && filteredCustomers.length === 0 && !loadingCustomers && (
                <p className="mt-1.5 text-xs text-gray-400">No customers found for "{searchQuery}"</p>
              )}
            </div>
          )}
        </div>

        {/* ── Related Request ── */}
        {selectedCustomer && !initRequestId && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Related Request <span className="text-xs text-gray-400 font-normal">(optional)</span>
            </label>
            {loadingRequests ? (
              <p className="text-xs text-gray-400">Loading requests…</p>
            ) : customerRequests.length === 0 ? (
              <p className="text-xs text-gray-400 italic">No active requests for this customer.</p>
            ) : (
              <Select
                value={selectedRequestId}
                onChange={(e) => setSelectedRequestId(e.target.value)}
                options={[
                  { value: '', label: '— Walk-in / No specific request —' },
                  ...customerRequests.map((r) => ({
                    value: r.id,
                    label: `${r.requestId} — Plot ${r.plotNumber ?? '?'}, ${r.sectorNo ?? ''} [${r.status}]`,
                  })),
                ]}
              />
            )}
          </div>
        )}

        {/* ── Department ── */}
        <Select
          label="Department *"
          value={departmentId}
          onChange={(e) => setDepartmentId(e.target.value)}
          options={[
            { value: '', label: departments.length === 0 ? 'Loading departments…' : '— Select department —' },
            ...departments.map((d) => ({ value: d.departmentId, label: d.departmentName })),
          ]}
        />

        {/* ── Schedule ── */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <Input
            label="Date *"
            type="date"
            value={appointmentDate}
            onChange={(e) => setAppointmentDate(e.target.value)}
            min={new Date().toISOString().split('T')[0]}
          />
          <Input
            label="Start Time *"
            type="time"
            value={startTime}
            onChange={(e) => setStartTime(e.target.value)}
          />
          <Select
            label="Duration"
            value={String(duration)}
            onChange={(e) => setDuration(Number(e.target.value))}
            options={[
              { value: '15', label: '15 min' },
              { value: '30', label: '30 min' },
              { value: '45', label: '45 min' },
              { value: '60', label: '1 hour' },
              { value: '90', label: '1.5 hours' },
            ]}
          />
        </div>

        {/* ── Assign Employee ── */}
        {departmentId && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Assign Employee
              {deptName && <span className="text-gray-400 font-normal"> — {deptName}</span>}
              <span className="text-xs text-gray-400 font-normal"> (optional)</span>
            </label>
            {loadingUsers ? (
              <p className="text-xs text-gray-400">Loading employees…</p>
            ) : allDeptUsers.length === 0 ? (
              <p className="text-xs text-gray-400">No active employees in this department.</p>
            ) : (
              <div className="space-y-1 max-h-44 overflow-y-auto border border-gray-200 rounded-lg p-2">
                <label className="flex items-center gap-2 p-2 rounded hover:bg-gray-50 cursor-pointer">
                  <input
                    type="radio"
                    name="employee"
                    value=""
                    checked={assignedEmpId === ''}
                    onChange={() => setAssignedEmpId('')}
                    className="accent-blue-600"
                  />
                  <span className="text-sm text-gray-500 italic">Unassigned / Any available</span>
                </label>
                {allDeptUsers.map((u) => {
                  const isAvailable = !appointmentDate || availableEmpIds.has(u.userId);
                  return (
                    <label
                      key={u.userId}
                      className={`flex items-center gap-2 p-2 rounded
                        ${isAvailable ? 'hover:bg-gray-50 cursor-pointer' : 'opacity-50 cursor-not-allowed'}`}
                    >
                      <input
                        type="radio"
                        name="employee"
                        value={u.userId}
                        checked={assignedEmpId === u.userId}
                        disabled={!isAvailable}
                        onChange={() => isAvailable && setAssignedEmpId(u.userId)}
                        className="accent-blue-600"
                      />
                      <div className="flex-1 min-w-0">
                        <span className="text-sm font-medium text-gray-800">{u.fullName}</span>
                        <span className="ml-2 text-xs text-gray-400">{u.roleName}</span>
                      </div>
                      {appointmentDate && (
                        isAvailable
                          ? <span className="text-xs text-green-600 font-medium shrink-0">✓ Free</span>
                          : <span className="text-xs text-red-400 shrink-0">Busy</span>
                      )}
                    </label>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* ── Attendees ── */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Select
            label="Number of Persons * (max 2)"
            value={String(numberOfPersons)}
            onChange={(e) => setNumberOfPersons(Number(e.target.value))}
            options={[
              { value: '1', label: '1 Person' },
              { value: '2', label: '2 Persons' },
            ]}
          />
          <Input
            label={numberOfPersons === 2 ? 'Attendee Names (comma-separated)' : 'Attendee Name'}
            placeholder={numberOfPersons === 2 ? 'Name 1, Name 2' : 'Full name'}
            value={attendeeNames}
            onChange={(e) => setAttendeeNames(e.target.value)}
          />
        </div>

        <Input
          label="Purpose / Agenda"
          placeholder="e.g. Review architectural plan, payment query…"
          value={purpose}
          onChange={(e) => setPurpose(e.target.value)}
        />

        <Textarea
          label="Additional Notes"
          placeholder="Any special instructions or context…"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
        />
      </div>
    </Modal>
  );
};
