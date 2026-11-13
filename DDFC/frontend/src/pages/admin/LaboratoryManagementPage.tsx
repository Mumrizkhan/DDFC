import React, { useEffect, useState } from 'react';
import { Pencil, Plus } from 'lucide-react';
import { toast } from 'react-toastify';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { soilTestService, type Laboratory } from '../../services/soilTest';

export const LaboratoryManagementPage: React.FC = () => {
  const [labs, setLabs] = useState<Laboratory[]>([]);
  const [editing, setEditing] = useState<Laboratory | null>(null);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [isActive, setIsActive] = useState(true);

  const load = async () => {
    try { setLabs(await soilTestService.labs(true)); }
    catch { toast.error('Unable to load labs'); }
    finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  const openForm = (lab: Laboratory | null) => {
    setEditing(lab);
    setName(lab?.name ?? '');
    setEmail(lab?.email ?? '');
    setPhone(lab?.phone ?? '');
    setIsActive(lab?.isActive ?? true);
    setOpen(true);
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!name.trim()) return;
    setSaving(true);
    try {
      const data = { name: name.trim(), email: email.trim() || null, phone: phone.trim() || null, isActive };
      if (editing) await soilTestService.updateLab(editing.id, data);
      else await soilTestService.createLab(data);
      setOpen(false);
      await load();
      toast.success('Lab saved');
    } catch (error) {
      toast.error((error as { message?: string }).message ?? 'Unable to save lab');
    } finally { setSaving(false); }
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-bold text-gray-900">Laboratories</h1>
        <Button onClick={() => openForm(null)} icon={<Plus size={16} />}>Add Lab</Button>
      </div>
      <Card>
        {loading ? <p className="text-sm text-gray-500">Loading labs...</p> : labs.length === 0 ? (
          <p className="text-sm text-gray-500">No labs configured.</p>
        ) : (
          <div className="divide-y divide-gray-100">
            {labs.map((lab) => (
              <div key={lab.id} className="flex items-start justify-between gap-4 py-3">
                <div className="min-w-0">
                  <p className="font-medium text-gray-900 break-words">{lab.name} <span className="text-xs font-normal text-gray-500">{lab.isActive ? 'Active' : 'Inactive'}</span></p>
                  <p className="text-sm text-gray-500 break-words">{[lab.email, lab.phone].filter(Boolean).join(' | ') || 'No contact details'}</p>
                </div>
                <button type="button" title="Edit lab" aria-label={`Edit ${lab.name}`} onClick={() => openForm(lab)} className="text-blue-600 p-2"><Pencil size={16} /></button>
              </div>
            ))}
          </div>
        )}
      </Card>
      <Modal isOpen={open} onClose={() => setOpen(false)} title={editing ? 'Edit Lab' : 'Add Lab'}>
        <form onSubmit={save} className="space-y-4">
          <Input label="Lab Name" required maxLength={200} value={name} onChange={(event) => setName(event.target.value)} />
          <Input label="Email" type="email" maxLength={320} value={email} onChange={(event) => setEmail(event.target.value)} />
          <Input label="Phone" maxLength={50} value={phone} onChange={(event) => setPhone(event.target.value)} />
          <label className="flex items-center gap-2 text-sm text-gray-700"><input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} /> Active</label>
          <Button type="submit" loading={saving} disabled={!name.trim()}>Save Lab</Button>
        </form>
      </Modal>
    </div>
  );
};