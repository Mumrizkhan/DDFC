import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { X } from 'lucide-react';
import { Card } from '../ui/Card';
import { Input } from '../ui/Input';
import { Button } from '../ui/Button';
import { toast } from 'react-toastify';
import api from '../../services/api';
import type { Customer } from '../../types';

interface AddCustomerModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCustomerAdded: (customer: Customer) => void;
}

type AddCustomerForm = {
  fullName: string;
  cnic: string;
  phoneNumber: string;
  email: string;
};

export const AddCustomerModal: React.FC<AddCustomerModalProps> = ({ isOpen, onClose, onCustomerAdded }) => {
  const [loading, setLoading] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<AddCustomerForm>({
    defaultValues: { fullName: '', cnic: '', phoneNumber: '', email: '' },
  });

  const onSubmit = async (data: AddCustomerForm) => {
    try {
      setLoading(true);
      const response = await api.post<Customer>('/admin/customers', data);
      toast.success('Customer created successfully!');
      onCustomerAdded(response.data);
      reset();
      onClose();
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } };
      toast.error(error.response?.data?.message || 'Failed to create customer');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <Card className="w-full max-w-md">
        <div className="flex justify-between items-center mb-6 border-b pb-4">
          <h2 className="text-xl font-bold text-gray-900">Add New Customer</h2>
          <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded transition-colors">
            <X size={20} className="text-gray-600" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Full Name <span className="text-red-500">*</span>
            </label>
            <Input
              {...register('fullName', { required: 'Full name is required' })}
              placeholder="Enter full name"
              error={errors.fullName?.message}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              CNIC <span className="text-red-500">*</span>
            </label>
            <Input
              {...register('cnic', { required: 'CNIC is required' })}
              placeholder="e.g., 12345-1234567-1"
              error={errors.cnic?.message}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number <span className="text-red-500">*</span>
            </label>
            <Input
              {...register('phoneNumber', { required: 'Phone number is required' })}
              placeholder="e.g., 03001234567"
              error={errors.phoneNumber?.message}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <Input
              {...register('email')}
              type="email"
              placeholder="e.g., customer@example.com"
              error={errors.email?.message}
            />
          </div>

          <div className="flex gap-3 pt-4 border-t">
            <Button variant="secondary" onClick={onClose} type="button">
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Creating...' : 'Create Customer'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};
