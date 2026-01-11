export interface CastMember {
  id: number;
  name: string;
  birthday: string | null;
}

export interface Show {
  id: number;
  name: string;
  cast: CastMember[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
