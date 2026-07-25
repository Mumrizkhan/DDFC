import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { User, Phone } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';
import { toast } from 'react-toastify';

const schema = z.object({
  phoneNumber: z
    .string()
    .regex(/^03\d{9}$/, 'Enter a valid Pakistani mobile number (e.g. 03001234567)'),
});
type FormValues = z.infer<typeof schema>;

export const ProfilePage: React.FC = () => {
  const { customer } = useAppSelector((s) => s.auth);
  const [editing, setEditing] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { phoneNumber: customer?.phoneNumber ?? '' },
  });

  const onSubmit = async (data: FormValues) => {
    // In a real app, dispatch an update phone thunk
    await new Promise((r) => setTimeout(r, 600));
    toast.success('Phone number updated');
    setEditing(false);
  };

  const handleCancel = () => {
    reset({ phoneNumber: customer?.phoneNumber ?? '' });
    setEditing(false);
  };

  return (
    <div className="max-w-xl mx-auto p-4 space-y-5">
      <h1 className="text-2xl font-bold text-gray-900">My Profile</h1>

      <Card>
        <div className="flex items-center gap-4 mb-6">
          <div className="w-16 h-16 rounded-full bg-dha-green flex items-center justify-center text-white text-2xl font-bold">
            {(customer?.fullName ?? 'C')[0]}
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{customer?.fullName ?? '—'}</p>
            <p className="text-sm text-gray-400">{customer?.cnic}</p>
          </div>
        </div>

        <dl className="space-y-4">
          <div className="flex items-center gap-3">
            <User size={16} className="text-gray-400" />
            <div>
              <dt className="text-xs text-gray-400">Full Name</dt>
              <dd className="text-sm font-medium text-gray-800">{customer?.fullName ?? '—'}</dd>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <User size={16} className="text-gray-400" />
            <div>
              <dt className="text-xs text-gray-400">CNIC</dt>
              <dd className="text-sm font-medium text-gray-800">{customer?.cnic ?? '—'}</dd>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <Phone size={16} className="text-gray-400" />
            <div className="flex-1">
              <dt className="text-xs text-gray-400 mb-1">Mobile Number</dt>
              {editing ? (
                <form onSubmit={handleSubmit(onSubmit)} className="flex items-start gap-2">
                  <Input
                    placeholder="03001234567"
                    error={errors.phoneNumber?.message}
                    {...register('phoneNumber')}
                    className="flex-1"
                  />
                  <Button type="submit" variant="primary" size="sm" loading={isSubmitting}>
                    Save
                  </Button>
                  <Button type="button" variant="ghost" size="sm" onClick={handleCancel}>
                    Cancel
                  </Button>
                </form>
              ) : (
                <div className="flex items-center gap-3">
                  <dd className="text-sm font-medium text-gray-800">
                    {customer?.phoneNumber ?? '—'}
                  </dd>
                  <button
                    className="text-xs text-dha-green hover:underline"
                    onClick={() => setEditing(true)}
                  >
                    Edit
                  </button>
                </div>
              )}
            </div>
          </div>
        </dl>
      </Card>
    </div>
  );
};
