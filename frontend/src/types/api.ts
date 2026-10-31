export interface ApiError {
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export interface AuthResponse {
  userId: number;
  fullName: string;
  email: string;
  token: string;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface UserProfile {
  id: number;
  fullName: string;
  email: string;
  createdAt: string;
}

export interface ResumeItem {
  id: number;
  fileName: string;
  uploadedAt: string;
  message: string;
}

export interface ResumeDetails extends ResumeItem {
  hasExtractedText: boolean;
  extractedTextLength: number;
}

export interface Job {
  id: number;
  title: string;
  companyName: string;
  description: string;
  requiredSkills: string;
  location: string;
  employmentType: string;
  createdAt: string;
}

export interface JobRequest {
  title: string;
  companyName: string;
  description: string;
  requiredSkills: string;
  location: string;
  employmentType: string;
}

export interface SkillMatchDetail {
  skill: string;
  isMatched: boolean;
  explanation: string;
}

export interface MatchResult {
  resumeId: number;
  resumeFileName: string;
  jobId: number;
  jobTitle: string;
  companyName: string;
  matchScore: number;
  skillMatchScore: number;
  descriptionRelevanceScore: number;
  matchingSkills: string[];
  missingSkills: string[];
  skillMatchDetails: SkillMatchDetail[];
  summary: string;
}

export interface MatchRequest {
  resumeId: number;
  jobId: number;
}

export interface AiAnalyticsCategoryScores {
  technicalSkills: number;
  experienceRelevance: number;
  educationAndCertifications: number;
  softSkillsAndLeadership: number;
  overallFit: number;
}

export interface AiAnalyticsResponse {
  resumeId: number;
  jobId: number;
  jobTitle: string;
  companyName: string;
  verdict: string;
  summary: string;
  categoryScores: AiAnalyticsCategoryScores;
  strengths: string[];
  improvements: string[];
  interviewFocusAreas: string[];
}
