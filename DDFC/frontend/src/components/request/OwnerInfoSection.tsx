import React from 'react';
import type { UseFormRegister, FieldErrors } from 'react-hook-form';
import { Input, Select } from '../ui/Input';
import type { FormValues } from './schema';

const TITLE_OPTIONS = [
  { value: 'Mr', label: 'Mr' },
  { value: 'Mrs', label: 'Mrs' },
  { value: 'Ms', label: 'Ms' },
  { value: 'Dr', label: 'Dr' },
];

const RELATION_OPTIONS = [
  { value: 'Self', label: 'Self' },
  { value: 'Father', label: 'Father' },
  { value: 'Mother', label: 'Mother' },
  { value: 'Spouse', label: 'Spouse' },
  { value: 'Guardian', label: 'Guardian' },
  { value: 'Other', label: 'Other' },
];

interface OwnerInfoSectionProps {
  register: UseFormRegister<FormValues>;
  errors: FieldErrors<FormValues>;
  watchedRelation: string;
}

export const OwnerInfoSection: React.FC<OwnerInfoSectionProps> = ({ register, errors, watchedRelation }) => (
  <>
    <div className="border border-gray-200 rounded-lg p-4 space-y-4">
      <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
        Registered in the name of
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Title <span className="text-red-500">*</span>
          </label>
          <Select
            {...register('ownerTitle')}
            options={TITLE_OPTIONS}
            error={errors.ownerTitle?.message}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Owner Name <span className="text-red-500">*</span>
          </label>
          <Input
            {...register('ownerName')}
            placeholder="Full name as per CNIC"
            error={errors.ownerName?.message}
          />
        </div>
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Son/Daughter/Wife of <span className="text-red-500">*</span>
          </label>
          <Input
            {...register('sonDaughterWifeOf')}
            placeholder="Full name"
            error={errors.sonDaughterWifeOf?.message}
          />
        </div>
      </div>
    </div>

    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">
        Relation <span className="text-red-500">*</span>
      </label>
      <Select
        {...register('guardianRelation')}
        options={RELATION_OPTIONS}
        error={errors.guardianRelation?.message}
      />
    </div>

    {watchedRelation && watchedRelation !== 'Self' && (
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Authorized Representative Name <span className="text-red-500">*</span>
        </label>
        <Input
          {...register('authorizedPersonName')}
          placeholder="Full name of authorized person"
          error={errors.authorizedPersonName?.message}
        />
      </div>
    )}

    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">
        Contractor <span className="text-gray-400 text-xs font-normal">(optional)</span>
      </label>
      <Input
        {...register('contractor')}
        placeholder="Contractor name"
      />
    </div>
  </>
);
