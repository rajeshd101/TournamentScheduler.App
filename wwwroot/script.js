window.setTheme = (theme) => {
    if (theme === 'system') {
        document.documentElement.removeAttribute('data-theme');
    } else {
        document.documentElement.setAttribute('data-theme', theme);
    }
};

window.triggerPrint = () => {
    window.print();
};

window.downloadFile = (filename, base64Content) => {
    const link = document.createElement('a');
    link.href = 'data:text/csv;base64,' + base64Content;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
