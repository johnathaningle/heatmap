


self.onmessage = function (data) {
    var dates = [];
    var heatmapData = JSON.parse(data.data);
    heatmapData.forEach(element => {
        if(dates.indexOf(element) < 0) {
            dates.push(element);
            postMessage(element);
        }

    });
}
