import React, { useEffect, useState } from 'react';
import { Plus, Edit2 } from 'lucide-react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchDepartments,
  createDepartment,
  updateDepartment,
} from '../../store/slices/adminSlice';
import { Card } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { Input } from '../../components/ui/Input';
import { PageLoader } from '../../components/ui/LoadingSpinner';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'react-toastify';
import type { Department } from '../../types';

const schema = z.object({
  departmentName: z.string().min(1, 'Name required'),
  departmentCode: z.string().min(1, 'Code required'),
  headUserId: z.string().optional(),
});
type FormData = z.infer<typeof schema>;

export const DepartmentManagementPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { departments, loading, actionLoading } = useAppSelector((s) => s.admin);
  const [showModal, setShowModal] = useState(false);
  const [editDept, setEditDept] = useState<Department | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  useEffect(() => {
    dispatch(fetchDepartments());
  }, [dispatch]);

  const openCreate = () => {
    setEditDept(null);
    reset({});
    setShowModal(true);
  };

  const openEdit = (dept: Department) => {
    setEditDept(dept);
    reset({
      departmentName: dept.departmentName,
      departmentCode: dept.departmentCode,
      headUserId: dept.headUserId ?? '',
    });
    setShowModal(true);
  };

  const onSubmit = async (data: FormData) => {
    if (editDept) {
      await dispatch(updateDepartment({ deptId: editDept.departmentId, data })).unwrap();
      toast.success('Department updated');
    } else {
      await dispatch(createDepartment(data)).unwrap();
      toast.success('Department created');
    }
    setShowModal(false);
  };

  if (loading) return <PageLoader />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Department Management</h1>
          <p className="text-gray-500 text-sm mt-1">{departments.length} departments</p>
        </div>
        <Button onClick={openCreate}>
          <Plus size={16} /> Add Department
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {departments.map((dept) => (
          <div
            key={dept.departmentId}
            className="bg-white rounded-xl border border-gray-200 p-5 hover:shadow-md transition-shadow"
          >
            <div className="flex items-start justify-between mb-3">
              <div>
                <h3 className="font-semibold text-gray-900">{dept.departmentName}</h3>
                <Badge variant="info" className="mt-1">
                  {dept.departmentCode}
                </Badge>
              </div>
              <button
                onClick={() => openEdit(dept)}
                className="text-blue-600 hover:text-blue-800"
              >
                <Edit2 size={16} />
              </button>
            </div>
            <div className="space-y-1 text-sm text-gray-500">
              <p>Manager: {dept.headName ?? '—'}</p>
              <p>Employees: {dept.employeeCount ?? 0}</p>
              <p>Active Requests: {dept.activeRequests ?? 0}</p>
            </div>
          </div>
        ))}
      </div>

      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title={editDept ? 'Edit Department' : 'Create Department'}
        footer={
          <>
            <Button variant="outline" onClick={() => setShowModal(false)}>
              Cancel
            </Button>
            <Button onClick={handleSubmit(onSubmit)} loading={actionLoading}>
              {editDept ? 'Save' : 'Create'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Department Name"
            error={errors.departmentName?.message}
            {...register('departmentName')}
          />
          <Input
            label="Department Code"
            error={errors.departmentCode?.message}
            {...register('departmentCode')}
          />
          <Input
            label="Head User ID (optional)"
            {...register('headUserId')}
          />
        </div>
      </Modal>
    </div>
  );
};
