import { z } from 'zod';

export const schema = z.object({
  customerId: z.string().min(1, 'Customer is required'),
  sectorNo: z.string().min(1, 'Sector is required'),
  phaseNo: z.string().min(1, 'Phase is required'),
  fileNo: z.string().min(1, 'File number is required'),
  membershipDPRNo: z.string().min(1, 'Membership/DPR No is required'),
  ownerTitle: z.string().min(1, 'Title is required'),
  ownerName: z.string().min(1, 'Owner name is required'),
  sonDaughterWifeOf: z.string().min(1, 'Son/Daughter/Wife of is required'),
  guardianRelation: z.string().min(1, 'Relation is required'),
  contractor: z.string().optional(),
  authorizedPersonName: z.string().optional(),
}).superRefine((data, ctx) => {
  if (data.guardianRelation && data.guardianRelation !== 'Self' && !data.authorizedPersonName?.trim()) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Authorized representative name is required',
      path: ['authorizedPersonName'],
    });
  }
});

export type FormValues = z.infer<typeof schema>;
