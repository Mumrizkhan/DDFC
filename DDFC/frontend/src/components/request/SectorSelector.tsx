import React from 'react';

interface SectorSelectorProps {
  sectors: string[];
  loading: boolean;
  selectedPhase: string;
  selectedSector: string;
  onSelect: (sector: string) => void;
  error?: string;
}

export const SectorSelector: React.FC<SectorSelectorProps> = ({
  sectors,
  loading,
  selectedPhase,
  selectedSector,
  onSelect,
  error,
}) => (
  <div>
    <label className="block text-sm font-medium text-gray-700 mb-2">
      Sector <span className="text-red-500">*</span>
    </label>
    {!selectedPhase ? (
      <p className="text-sm text-gray-400 italic">Select a phase first</p>
    ) : loading ? (
      <p className="text-sm text-gray-500">Loading sectors...</p>
    ) : (
      <div className="flex flex-wrap gap-2">
        {sectors.map((s) => (
          <label
            key={s}
            className={`cursor-pointer select-none px-4 py-2 rounded-full border text-sm font-medium transition-colors ${
              selectedSector === s
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-700 border-gray-300 hover:border-blue-400 hover:text-blue-600'
            }`}
          >
            <input
              type="radio"
              className="sr-only"
              value={s}
              checked={selectedSector === s}
              onChange={() => onSelect(s)}
            />
            Sector {s}
          </label>
        ))}
      </div>
    )}
    {error && <p className="text-red-500 text-sm mt-1">{error}</p>}
  </div>
);
