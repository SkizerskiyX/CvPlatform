/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

declare module 'react-tagcloud' {
  export type Tag = { value: string; count: number };

  export function TagCloud(props: {
    tags: Tag[];
    minSize: number;
    maxSize: number;
    onClick?: (tag: Tag) => void;
  }): JSX.Element;
}