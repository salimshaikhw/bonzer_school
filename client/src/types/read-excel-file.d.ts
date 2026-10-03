declare module 'read-excel-file/browser' {
  type Row = unknown[]
  function readXlsxFile(file: File | Blob): Promise<Row[]>
  export default readXlsxFile
}
