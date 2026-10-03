const watermarkText = 'CONFIDENTIAL';

const createWatermark = (viewerName: string): HTMLCanvasElement => {
  if (!viewerName.trim()) throw new Error('The logged-in user name is unavailable');
  const canvas = document.createElement('canvas');
  canvas.width = 1000;
  canvas.height = 360;
  const context = canvas.getContext('2d');
  if (!context) throw new Error('Document watermarking is unavailable');
  context.translate(canvas.width / 2, canvas.height / 2);
  context.rotate(-Math.PI / 9);
  context.textAlign = 'center';
  context.fillStyle = '#5b6573';
  context.font = 'bold 56px Arial';
  context.fillText(watermarkText, 0, -12);
  context.font = '36px Arial';
  context.fillText(viewerName.trim(), 0, 42, 860);
  return canvas;
};

const canvasBlob = (canvas: HTMLCanvasElement): Promise<Blob> => new Promise((resolve, reject) => {
  canvas.toBlob((blob) => blob ? resolve(blob) : reject(new Error('Could not watermark the document')), 'image/png');
});

export const watermarkPdf = async (file: Blob, viewerName: string): Promise<Blob> => {
  const { PDFDocument } = await import('pdf-lib');
  const pdf = await PDFDocument.load(await file.arrayBuffer());
  const watermark = await pdf.embedPng(await (await canvasBlob(createWatermark(viewerName))).arrayBuffer());
  for (const page of pdf.getPages()) {
    const width = Math.min(page.getWidth() * 0.8, page.getHeight() * 0.4 * watermark.width / watermark.height);
    const height = width * watermark.height / watermark.width;
    for (const position of [0.25, 0.5, 0.75]) {
      page.drawImage(watermark, {
        x: (page.getWidth() - width) / 2,
        y: page.getHeight() * position - height / 2,
        width,
        height,
        opacity: 0.22,
      });
    }
  }
  return new Blob([new Uint8Array(await pdf.save()).buffer], { type: 'application/pdf' });
};

export const watermarkImage = async (file: Blob, viewerName: string): Promise<Blob> => {
  const image = await createImageBitmap(file);
  try {
    const canvas = document.createElement('canvas');
    canvas.width = image.width;
    canvas.height = image.height;
    const context = canvas.getContext('2d');
    if (!context) throw new Error('Document watermarking is unavailable');
    context.drawImage(image, 0, 0);
    const watermark = createWatermark(viewerName);
    const width = Math.min(image.width * 0.85, image.height * 0.85 * watermark.width / watermark.height, 1000);
    const height = width * watermark.height / watermark.width;
    context.globalAlpha = 0.28;
    for (let position = height / 2; position < image.height; position += height * 1.5) {
      context.drawImage(watermark, (image.width - width) / 2, position - height / 2, width, height);
    }
    return await canvasBlob(canvas);
  } finally {
    image.close();
  }
};

export const watermarkHtml = (html: string, viewerName: string): string => {
  if (!viewerName.trim()) throw new Error('The logged-in user name is unavailable');
  const parsed = new DOMParser().parseFromString(html, 'text/html');
  const style = parsed.createElement('style');
  style.textContent = `
    .document-confidential-watermark { position: fixed; inset: 0; z-index: 2147483647; pointer-events: none;
      display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); grid-template-rows: repeat(3, minmax(0, 1fr));
      opacity: .2; color: #5b6573; print-color-adjust: exact; -webkit-print-color-adjust: exact; }
    .document-confidential-watermark > div { display: flex; flex-direction: column; justify-content: center;
      align-items: center; transform: rotate(-20deg); padding: 16px; min-width: 0; text-align: center;
      font-family: Arial, sans-serif; font-size: 16px; line-height: 1.4; overflow-wrap: anywhere; }
    .document-confidential-watermark strong { font-size: 28px; }
    @media print { .document-confidential-watermark { position: fixed; } }
  `;
  parsed.head.append(style);
  const layer = parsed.createElement('div');
  layer.className = 'document-confidential-watermark';
  for (let index = 0; index < 6; index++) {
    const tile = parsed.createElement('div');
    const label = parsed.createElement('strong');
    label.textContent = watermarkText;
    const name = parsed.createElement('span');
    name.textContent = viewerName.trim();
    tile.append(label, name);
    layer.append(tile);
  }
  parsed.body.append(layer);
  return '<!DOCTYPE html>' + parsed.documentElement.outerHTML;
};

const openPreview = (): Window => {
  const preview = window.open('', '_blank');
  if (!preview) throw new Error('Please allow popups for this site and try again');
  preview.opener = null;
  preview.document.body.textContent = 'Loading document...';
  return preview;
};

export const viewWatermarkedFile = async (url: string, viewerName: string): Promise<void> => {
  const preview = openPreview();
  try {
    const response = await fetch(url);
    if (!response.ok) throw new Error('Could not load the document');
    const file = await response.blob();
    const path = new URL(url, window.location.href).pathname.toLowerCase();
    let stamped: Blob;
    if (file.type === 'application/pdf' || path.endsWith('.pdf')) {
      stamped = await watermarkPdf(file, viewerName);
    } else if (['image/png', 'image/jpeg'].includes(file.type) || /\.(png|jpe?g)$/.test(path)) {
      stamped = await watermarkImage(file, viewerName);
    } else {
      throw new Error('Watermarked previews are available for PDF, JPG, and PNG documents');
    }
    if (preview.closed) return;
    const objectUrl = URL.createObjectURL(stamped);
    preview.location.replace(objectUrl);
    const cleanup = window.setInterval(() => {
      if (preview.closed) {
        URL.revokeObjectURL(objectUrl);
        window.clearInterval(cleanup);
      }
    }, 1000);
  } catch (error) {
    preview.close();
    throw error;
  }
};

export const viewWatermarkedHtml = async (loadHtml: () => Promise<string>, viewerName: string): Promise<void> => {
  const preview = openPreview();
  try {
    const html = watermarkHtml(await loadHtml(), viewerName);
    if (preview.closed) return;
    preview.document.open();
    preview.document.write(html);
    preview.document.close();
  } catch (error) {
    preview.close();
    throw error;
  }
};