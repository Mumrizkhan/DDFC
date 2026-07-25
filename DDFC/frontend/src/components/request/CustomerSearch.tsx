import React, { useState, useMemo } from 'react';
import { UserPlus, X } from 'lucide-react';
import { Input } from '../ui/Input';
import { Button } from '../ui/Button';
import { AddCustomerModal } from './AddCustomerModal';
import type { Customer } from '../../types';

interface CustomerSearchProps {
  customers: Customer[];
  loadingCustomers: boolean;
  selectedCustomerId: string;
  onSelectCustomer: (customerId: string) => void;
  onCustomerAdded: (customer: Customer) => void;
  error?: string;
}

export const CustomerSearch: React.FC<CustomerSearchProps> = ({
  customers,
  loadingCustomers,
  selectedCustomerId,
  onSelectCustomer,
  onCustomerAdded,
  error,
}) => {
  const [searchInput, setSearchInput] = useState('');
  const [showAddModal, setShowAddModal] = useState(false);

  const filteredCustomers = useMemo(() => {
    if (!searchInput.trim()) return [];
    const q = searchInput.toLowerCase().trim();
    return customers.filter(
      (c) =>
        c.cnic.toLowerCase().includes(q) ||
        c.phoneNumber.toLowerCase().includes(q) ||
        c.fullName.toLowerCase().includes(q)
    );
  }, [searchInput, customers]);

  const selectedCustomer = customers.find((c) => c.customerId === selectedCustomerId);

  const handleCustomerAdded = (customer: Customer) => {
    onCustomerAdded(customer);
    onSelectCustomer(customer.customerId);
    setSearchInput('');
  };

  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">
        Find Customer by CNIC or Phone <span className="text-red-500">*</span>
      </label>

      {!selectedCustomer && (
        <div className="relative">
          <Input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder="Enter CNIC (e.g., 12345-1234567-1) or phone number (e.g., 03001234567)"
            disabled={loadingCustomers}
          />
          {searchInput && filteredCustomers.length === 0 && (
            <div className="mt-2 p-3 bg-gray-50 border border-gray-200 rounded-lg">
              <p className="text-sm text-gray-600 mb-2">No customer found with "{searchInput}"</p>
              <Button
                type="button"
                onClick={() => setShowAddModal(true)}
                icon={<UserPlus size={16} />}
                variant="secondary"
                className="w-full"
              >
                Add New Customer
              </Button>
            </div>
          )}
          {filteredCustomers.length > 0 && (
            <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-300 rounded-lg shadow-lg z-10">
              {filteredCustomers.map((customer) => (
                <button
                  key={customer.customerId}
                  type="button"
                  onClick={() => {
                    onSelectCustomer(customer.customerId);
                    setSearchInput('');
                  }}
                  className="w-full text-left px-4 py-2 hover:bg-blue-50 border-b last:border-b-0 transition-colors"
                >
                  <div className="font-medium text-gray-900">{customer.fullName}</div>
                  <div className="text-sm text-gray-600">
                    CNIC: {customer.cnic} | Phone: {customer.phoneNumber}
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {selectedCustomer && (
        <div className="mt-2 p-3 bg-green-50 border border-green-200 rounded-lg flex items-start justify-between gap-2">
          <div className="space-y-0.5">
            <p className="text-sm font-semibold text-green-800">✓ {selectedCustomer.fullName}</p>
            <p className="text-xs text-green-700">CNIC: {selectedCustomer.cnic}</p>
            <p className="text-xs text-green-700">Mobile: {selectedCustomer.phoneNumber}</p>
          </div>
          <button
            type="button"
            onClick={() => onSelectCustomer('')}
            className="p-1 rounded hover:bg-green-200 text-green-700 transition-colors flex-shrink-0"
            title="Change customer"
          >
            <X size={14} />
          </button>
        </div>
      )}

      {error && <p className="text-red-500 text-sm mt-1">{error}</p>}

      <AddCustomerModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        onCustomerAdded={handleCustomerAdded}
      />
    </div>
  );
};
