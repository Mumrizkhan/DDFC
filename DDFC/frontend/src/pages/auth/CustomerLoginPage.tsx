import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { toast } from 'react-toastify';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  customerRequestOtp,
  customerVerifyOtp,
  clearError,
  resetOtpSent,
} from '../../store/slices/authSlice';
import { Input } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';

const cnicSchema = z.object({
  cnic: z
    .string()
    .regex(/^\d{13}$/, 'CNIC must be exactly 13 digits (no dashes)'),
});

const otpSchema = z.object({
  otp: z.string().min(4, 'Enter the OTP sent to your phone'),
});

type CnicForm = z.infer<typeof cnicSchema>;
type OtpForm = z.infer<typeof otpSchema>;

export const CustomerLoginPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { loading, error, otpSent, customer } = useAppSelector((s) => s.auth);
  const [cnic, setCnic] = useState('');

  const cnicForm = useForm<CnicForm>({ resolver: zodResolver(cnicSchema) });
  const otpForm = useForm<OtpForm>({ resolver: zodResolver(otpSchema) });

  useEffect(() => {
    if (customer) navigate('/portal');
  }, [customer, navigate]);

  useEffect(() => {
    if (error) {
      toast.error(error);
      dispatch(clearError());
    }
  }, [error, dispatch]);

  const onCnicSubmit = (data: CnicForm) => {
    setCnic(data.cnic);
    dispatch(customerRequestOtp({ cnic: data.cnic }));
  };

  const onOtpSubmit = (data: OtpForm) => {
    dispatch(customerVerifyOtp({ cnic, otp: data.otp }));
  };

  const handleBack = () => {
    dispatch(resetOtpSent());
    cnicForm.reset();
    otpForm.reset();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-green-900 to-teal-700 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-green-500 mb-4">
            <span className="text-white text-2xl font-bold">D</span>
          </div>
          <h1 className="text-2xl font-bold text-white">DDFC Customer Portal</h1>
          <p className="text-green-300 mt-1">DHA Peshawar · Plot Owners</p>
        </div>

        <div className="bg-white rounded-2xl shadow-2xl p-8">
          {!otpSent ? (
            <>
              <h2 className="text-lg font-semibold text-gray-900 mb-2">
                Enter Your CNIC
              </h2>
              <p className="text-sm text-gray-500 mb-6">
                We'll send a one-time password to your registered phone number.
              </p>
              <form onSubmit={cnicForm.handleSubmit(onCnicSubmit)} className="space-y-4">
                <Input
                  label="CNIC (13 digits, no dashes)"
                  placeholder="3420112345678"
                  maxLength={13}
                  error={cnicForm.formState.errors.cnic?.message}
                  {...cnicForm.register('cnic')}
                />
                <Button type="submit" className="w-full" loading={loading}>
                  Send OTP
                </Button>
              </form>
            </>
          ) : (
            <>
              <h2 className="text-lg font-semibold text-gray-900 mb-2">
                Enter OTP
              </h2>
              <p className="text-sm text-gray-500 mb-6">
                A 6-digit code was sent to your registered phone number.
              </p>
              <form onSubmit={otpForm.handleSubmit(onOtpSubmit)} className="space-y-4">
                <Input
                  label="One-Time Password"
                  placeholder="123456"
                  maxLength={8}
                  error={otpForm.formState.errors.otp?.message}
                  {...otpForm.register('otp')}
                />
                <Button type="submit" className="w-full" loading={loading}>
                  Verify & Login
                </Button>
                <button
                  type="button"
                  onClick={handleBack}
                  className="w-full text-sm text-gray-500 hover:text-gray-700"
                >
                  ← Back
                </button>
              </form>
            </>
          )}
        </div>

        <p className="text-center text-green-300 text-sm mt-6">
          DHA Peshawar · DDFC Workflow System v1.0
        </p>
      </div>
    </div>
  );
};
