import React from 'react';
import { Loader2 } from 'lucide-react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  text?: string;
}

const sizeMap = { sm: 16, md: 24, lg: 40 };

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  text,
}) => (
  <div className="flex flex-col items-center justify-center gap-2 py-8">
    <Loader2
      size={sizeMap[size]}
      className="animate-spin text-blue-600"
    />
    {text && <p className="text-sm text-gray-500">{text}</p>}
  </div>
);

export const PageLoader: React.FC = () => (
  <div className="flex items-center justify-center min-h-[60vh]">
    <LoadingSpinner size="lg" text="Loading..." />
  </div>
);
