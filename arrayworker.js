

self.onmessage = function (data) {
    var distinct = function(value, index, self) {
        return self.indexOf(value) === index;
    }
    var heatmapData = JSON.parse(data.data);
    var data = heatmapData.map(x => x.date);
    data = data.filter(distinct);
    console.log(data);
    data.forEach(element => {
        postMessage(element);
    });
}
