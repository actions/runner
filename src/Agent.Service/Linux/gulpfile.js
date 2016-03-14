var fs = require('fs');
var path = require('path');
var gulp = require('gulp');
var del = require('del');
var mocha = require('gulp-mocha');
var tar = require('gulp-tar');
var gzip = require('gulp-gzip');
var merge = require('merge2');
var minimist = require('minimist');
var typescript = require('gulp-tsc');

var layoutbin = path.join(__dirname, '../../../_layout/bin');
var testRoot = path.join(__dirname, '_test');
var testPath = path.join(testRoot, 'test');
var buildPath = path.join(__dirname, '_build');

gulp.task('clean', function (done) {
    del([buildPath], done);
});

gulp.task('build', ['clean'], function () {
    return gulp.src(['*.ts'])
	.pipe(typescript())
	.pipe(gulp.dest(buildPath));
});

gulp.task('layout', ['clean', 'build'], function (done) {
    // TODO package this correctly, this works for devs now
    return merge([
    gulp.src([path.join(buildPath, '**')]).pipe(gulp.dest(layoutbin)),
    gulp.src(['package.json']).pipe(gulp.dest(layoutbin)),
    gulp.src(['vsts.agent.service.template']).pipe(gulp.dest(layoutbin))
    ]);
});

gulp.task('default', ['build', 'layout']);
