import React, { useEffect, useState } from 'react';
import { CalendarDays } from 'lucide-react';
import { toast } from 'react-toastify';
import { Button } from '../../../components/ui/Button';
import { Input, Select, Textarea } from '../../../components/ui/Input';
import { soilTestService, type Laboratory, type SoilTestSchedule } from '../../../services/soilTest';

interface Props {
  requestId: string;
  onScheduleChange?: React.Dispatch<React.SetStateAction<SoilTestSchedule | null>>;
}

const localDateTime = (value: string) => {
  const date = new Date(value);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};

export const SoilTestScheduleSection: React.FC<Props> = ({ requestId, onScheduleChange }) => {
  const [labs, setLabs] = useState<Laboratory[]>([]);
  const [schedule, setSchedule] = useState<SoilTestSchedule | null>(null);
  const [laboratoryId, setLaboratoryId] = useState('');
  const [scheduledAt, setScheduledAt] = useState('');
  const [location, setLocation] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let active = true;
    Promise.all([soilTestService.labs(), soilTestService.schedule(requestId)])
      .then(([availableLabs, existing]) => {
        if (!active) return;
        setLabs(availableLabs);
        setSchedule(existing);
        onScheduleChange?.(existing);
        if (existing) {
          setLaboratoryId(existing.laboratoryId);
          setScheduledAt(localDateTime(existing.scheduledAt));
          setLocation(existing.location ?? '');
          setNotes(existing.notes ?? '');
        }
      })
      .catch(() => { if (active) toast.error('Unable to load soil test schedule'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [requestId, onScheduleChange]);

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!laboratoryId || !scheduledAt || Number.isNaN(new Date(scheduledAt).getTime())) return;
    setSaving(true);
    try {
      const result = await soilTestService.saveSchedule(requestId, {
        laboratoryId, scheduledAt: new Date(scheduledAt).toISOString(),
        location: location.trim() || undefined, notes: notes.trim() || undefined,
      });
      setSchedule(result.schedule);
      onScheduleChange?.(result.schedule);
      toast.success('Soil test schedule saved');
      if (result.notificationWarning) toast.warn('DDFC or BCD has no active notification recipients.');
      if (result.emailWarning) toast.warn('Some email notifications could not be sent. Check SMTP and recipient email addresses.');
    } catch (error) {
      toast.error((error as { message?: string }).message ?? 'Unable to save schedule');
    } finally { setSaving(false); }
  };

  if (loading) return <p className="text-sm text-gray-500">Loading schedule...</p>;

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-gray-800 flex items-center gap-2"><CalendarDays size={16} /> Soil Test Schedule</h3>
      {schedule && (
        <div className="text-sm text-gray-700 border-l-2 border-emerald-500 pl-3 space-y-1">
          <p className="font-medium">{schedule.laboratoryName}</p>
          <p>{new Date(schedule.scheduledAt).toLocaleString()}</p>
          {schedule.location && <p>{schedule.location}</p>}
          {schedule.notes && <p className="break-words">{schedule.notes}</p>}
        </div>
      )}
      {labs.length === 0 ? <p className="text-sm text-amber-700">No active labs are configured.</p> : (
        <form onSubmit={save} className="space-y-3">
          <Select label="Laboratory" required value={laboratoryId} onChange={(event) => setLaboratoryId(event.target.value)} options={[
            { value: '', label: 'Select a lab' },
            ...labs.map((lab) => ({ value: lab.id, label: lab.name })),
          ]} />
          <Input label="Scheduled Date and Time" type="datetime-local" required value={scheduledAt} onChange={(event) => setScheduledAt(event.target.value)} />
          <Input label="Location (optional)" maxLength={500} value={location} onChange={(event) => setLocation(event.target.value)} />
          <Textarea label="Notes (optional)" rows={2} maxLength={2000} value={notes} onChange={(event) => setNotes(event.target.value)} />
          <Button type="submit" loading={saving} disabled={!laboratoryId || !scheduledAt} icon={<CalendarDays size={16} />}>
            {schedule ? 'Update Schedule' : 'Save Schedule'}
          </Button>
        </form>
      )}
    </div>
  );
};